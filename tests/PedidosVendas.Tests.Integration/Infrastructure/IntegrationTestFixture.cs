using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PedidosVendas.Infrastructure.Persistence;
using PedidosVendas.Tests.Integration.Infrastructure;
using Testcontainers.PostgreSql;

namespace PedidosVendas.Tests.Integration;

/// <summary>
/// Fixture compartilhada: um único container PostgreSQL + uma única instância da API
/// para toda a coleção de testes de integração. Aplica as migrations da Etapa 02+
/// (prova que a migration sobe com sucesso) antes de expor o client.
/// </summary>
public sealed class IntegrationTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder(
            image: "postgres:16-alpine")
        .WithDatabase("pedidosvendas")
        .WithUsername("pedidosvendas")
        .WithPassword("pedidosvendas_test_password")
        .Build();

    public PedidosVendasApiFactory Factory { get; private set; } = null!;

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        Factory = new PedidosVendasApiFactory(_postgres.GetConnectionString());

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PedidosVendasDbContext>();
            await db.Database.MigrateAsync();
        }

        // Seed do catálogo (o bloco Development do Program não roda em Testing).
        await PedidosVendas.Infrastructure.Persistence.Seed.FormaPagtoSeeder.SeedAsync(Factory.Services);

        Client = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    public const string Name = "Integration";
}
