using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PedidosVendas.Infrastructure.Persistence;

namespace PedidosVendas.Tests.Integration;

/// <summary>
/// Conexão com o PostgreSQL do compose (aqui via container efêmero com a mesma
/// imagem postgres:16-alpine): DbContext + NpgsqlDataSource executam contra o banco.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class DatabaseConnectionTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task DbContext_conecta_ao_postgresql()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PedidosVendasDbContext>();

        Assert.True(await db.Database.CanConnectAsync());
    }

    [Fact]
    public async Task NpgsqlDataSource_executa_select_1()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var dataSource = scope.ServiceProvider.GetRequiredService<NpgsqlDataSource>();

        await using var command = dataSource.CreateCommand("SELECT 1");
        var result = await command.ExecuteScalarAsync();

        Assert.Equal(1, Convert.ToInt32(result));
    }
}
