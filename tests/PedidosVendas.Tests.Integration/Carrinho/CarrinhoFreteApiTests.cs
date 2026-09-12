using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PedidosVendas.Infrastructure.Catalog;
using PedidosVendas.Tests.Integration.Infrastructure;

namespace PedidosVendas.Tests.Integration.Carrinho;

/// <summary>Etapa 05: cotação preliminar de frete sobre o carrinho existente.</summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class CarrinhoFreteApiTests(IntegrationTestFixture fixture)
{
    private static readonly Guid UserA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TenantA = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private const string BasePath = "/api/v1/carrinho";

    private HttpClient ClientFor(Guid userId, Guid tenantId)
    {
        var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwt.CreateToken(userId, tenantId));
        return client;
    }

    [Fact]
    public async Task Cotacao_para_carrinho_existente_retorna_valor_e_prazo()
    {
        using var client = ClientFor(UserA, TenantA);
        using (var post = await client.PostAsJsonAsync($"{BasePath}/itens",
                   new { idProduto = MockProdutoServiceClient.ProdutoDisponivelId, quantidade = 2 }))
        {
            Assert.Equal(HttpStatusCode.OK, post.StatusCode);
        }

        try
        {
            using var frete = await client.PostAsJsonAsync($"{BasePath}/frete",
                new { cepDestino = "01310-100" });

            Assert.Equal(HttpStatusCode.OK, frete.StatusCode);
            using var document = JsonDocument.Parse(await frete.Content.ReadAsStringAsync());
            var corpo = document.RootElement;
            Assert.Equal("01310100", corpo.GetProperty("cepDestino").GetString());
            Assert.Equal(16, corpo.GetProperty("valor").GetDecimal()); // 12 + 2 × 2
            Assert.Equal(2, corpo.GetProperty("prazoDiasUteis").GetInt32());
        }
        finally
        {
            await client.DeleteAsync($"{BasePath}/itens/{MockProdutoServiceClient.ProdutoDisponivelId}");
        }
    }

    [Fact]
    public async Task Cep_invalido_retorna_400_e_sem_carrinho_404()
    {
        using var client = ClientFor(UserA, TenantA);

        using (var frete = await client.PostAsJsonAsync($"{BasePath}/frete", new { cepDestino = "abc" }))
        {
            Assert.Equal(HttpStatusCode.BadRequest, frete.StatusCode);
        }

        using var clientNovo = ClientFor(Guid.NewGuid(), Guid.NewGuid());
        using (var frete = await clientNovo.PostAsJsonAsync($"{BasePath}/frete", new { cepDestino = "01310100" }))
        {
            Assert.Equal(HttpStatusCode.NotFound, frete.StatusCode);
        }
    }

    [Fact]
    public async Task Sem_JWT_frete_retorna_401()
    {
        using var client = fixture.Factory.CreateClient();

        using var response = await client.PostAsJsonAsync($"{BasePath}/frete", new { cepDestino = "01310100" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
