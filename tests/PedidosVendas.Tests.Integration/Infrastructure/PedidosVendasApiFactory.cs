using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using PedidosVendas.Tests.Integration.Infrastructure;

namespace PedidosVendas.Tests.Integration;

/// <summary>
/// Sobe a API em memória com:
/// - PostgreSQL apontando para o container efêmero do teste;
/// - validação JWT por chave simétrica (simula o microsserviço de Identity).
/// </summary>
public sealed class PedidosVendasApiFactory(string connectionString)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["Authentication:Jwt:SigningKey"] = TestJwt.SigningKey,
                ["Authentication:Jwt:ValidIssuer"] = TestJwt.Issuer,
                ["Authentication:Jwt:Audience"] = TestJwt.Audience,
                ["Authentication:Jwt:RequireHttpsMetadata"] = "false",
                ["Serilog:MinimumLevel:Default"] = "Warning"
            });
        });
    }
}
