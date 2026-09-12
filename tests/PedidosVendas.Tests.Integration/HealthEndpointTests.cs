using System.Net;
using PedidosVendas.Tests.Integration.Infrastructure;

namespace PedidosVendas.Tests.Integration;

/// <summary>
/// Smoke da Etapa 01: API sobe e health checks respondem com API + PostgreSQL no ar.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class HealthEndpointTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Health_agregado_retorna_200_com_api_e_banco_no_ar()
    {
        var response = await fixture.Client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Readiness_retorna_200_quando_postgres_acessivel()
    {
        var response = await fixture.Client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Liveness_retorna_200_independente_de_dependencias()
    {
        var response = await fixture.Client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
