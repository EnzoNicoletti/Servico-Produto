using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using PedidosVendas.API.Extensions;
using PedidosVendas.API.Middleware;
using PedidosVendas.Infrastructure;
using PedidosVendas.Infrastructure.Persistence;
using PedidosVendas.Infrastructure.Persistence.Seed;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddPedidosVendasInfrastructure(builder.Configuration);
    builder.Services.AddApiAuthentication();
    builder.Services.AddAuthorization();
    builder.Services.AddApiServices();

    var app = builder.Build();

    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });

    app.UseSerilogRequestLogging();

    // Ordem obrigatória: autenticação -> resolução de tenant -> autorização.
    app.UseAuthentication();
    app.UseMiddleware<TenantResolutionMiddleware>();
    app.UseAuthorization();

    app.MapControllers();

    // Liveness: apenas o processo está no ar (não consulta dependências).
    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains(DependencyInjection.LiveTag)
    });

    // Readiness: API + PostgreSQL prontos para receber tráfego.
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains(DependencyInjection.ReadyTag)
    });

    // Health agregado: todas as checagens (API + banco).
    app.MapHealthChecks("/health");

    // Somente em desenvolvimento as migrations são aplicadas automaticamente na subida
    // (mesmo padrão do Identity). Em produção, a migration é um passo controlado do
    // pipeline (ex.: dotnet ef database update).
    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PedidosVendasDbContext>();
        await db.Database.MigrateAsync();

        // Seed do catálogo (idempotente; mesmo padrão do RoleSeeder no Identity).
        await FormaPagtoSeeder.SeedAsync(scope.ServiceProvider);
    }

    app.Run();
}
catch (Exception ex) when (ex is not OperationCanceledException)
{
    Log.Fatal(ex, "A aplicação terminou de forma inesperada.");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

// Exposto para os testes de integração (WebApplicationFactory<Program>).
public partial class Program;
