using PedidosVendas.Tests.Integration.Infrastructure;
using Testcontainers.PostgreSql;

namespace PedidosVendas.Tests.Integration;

/// <summary>
/// Fixture compartilhada: um único container PostgreSQL + uma única instância da API
/// para toda a coleção de testes de integração.
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
