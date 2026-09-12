using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using PedidosVendas.API.Extensions;
using PedidosVendas.API.Middleware;
using PedidosVendas.Infrastructure;
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
