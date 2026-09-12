using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PedidosVendas.Infrastructure.Catalog;
using PedidosVendas.Tests.Integration.Infrastructure;

namespace PedidosVendas.Tests.Integration.Carrinho;

/// <summary>
/// Etapa 03: ciclo de vida do carrinho fim a fim contra PostgreSQL real,
/// com o client do Estoque em mock (Etapa 01 D2).
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class CarrinhoApiTests(IntegrationTestFixture fixture)
{
    private static readonly Guid UserA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TenantA = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid UserB = Guid.Parse("77777777-7777-7777-7777-777777777777");
    private static readonly Guid TenantC = Guid.Parse("99999999-9999-9999-9999-999999999999");

    private const string BasePath = "/api/v1/carrinho";

    private HttpClient ClientFor(Guid userId, Guid tenantId)
    {
        var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwt.CreateToken(userId, tenantId));
        return client;
    }

    private static async Task<JsonElement> LerCorpoAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    [Fact]
    public async Task Fluxo_completo_criar_adicionar_somar_atualizar_remover()
    {
        using var client = ClientFor(UserA, TenantA);

        // Obter cria o carrinho vazio.
        using (var get = await client.GetAsync(BasePath))
        {
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            var corpo = await LerCorpoAsync(get);
            Assert.Equal("Carrinho", corpo.GetProperty("status").GetString());
            Assert.Equal(0, corpo.GetProperty("valorTotal").GetDecimal());
            Assert.Equal(0, corpo.GetProperty("itens").GetArrayLength());
        }

        // Adicionar 2 unidades (mock: 99.90) totaliza 199.80.
        using (var post = await client.PostAsJsonAsync($"{BasePath}/itens",
                   new { idProduto = MockProdutoServiceClient.ProdutoDisponivelId, quantidade = 2 }))
        {
            Assert.Equal(HttpStatusCode.OK, post.StatusCode);
            var corpo = await LerCorpoAsync(post);
            Assert.Equal(199.80m, corpo.GetProperty("valorTotal").GetDecimal());
        }

        // Adicionar o mesmo produto soma a quantidade (1 linha, qtd 3, total 299.70).
        using (var post = await client.PostAsJsonAsync($"{BasePath}/itens",
                   new { idProduto = MockProdutoServiceClient.ProdutoDisponivelId, quantidade = 1 }))
        {
            var corpo = await LerCorpoAsync(post);
            var itens = corpo.GetProperty("itens").EnumerateArray().ToList();
            Assert.Single(itens);
            Assert.Equal(3, itens[0].GetProperty("quantidade").GetInt32());
            Assert.Equal(299.70m, corpo.GetProperty("valorTotal").GetDecimal());
        }

        // Atualizar quantidade recalcula.
        using (var put = await client.PutAsJsonAsync(
                   $"{BasePath}/itens/{MockProdutoServiceClient.ProdutoDisponivelId}",
                   new { quantidade = 1 }))
        {
            Assert.Equal(HttpStatusCode.OK, put.StatusCode);
            var corpo = await LerCorpoAsync(put);
            Assert.Equal(99.90m, corpo.GetProperty("valorTotal").GetDecimal());
        }

        // Remover esvazia e zera o total.
        using (var delete = await client.DeleteAsync(
                   $"{BasePath}/itens/{MockProdutoServiceClient.ProdutoDisponivelId}"))
        {
            Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
            var corpo = await LerCorpoAsync(delete);
            Assert.Equal(0, corpo.GetProperty("valorTotal").GetDecimal());
            Assert.Equal(0, corpo.GetProperty("itens").GetArrayLength());
        }
    }

    [Fact]
    public async Task Produto_inexistente_retorna_404_e_indisponivel_400()
    {
        using var client = ClientFor(UserA, TenantA);

        using (var post = await client.PostAsJsonAsync($"{BasePath}/itens",
                   new { idProduto = Guid.NewGuid(), quantidade = 1 }))
        {
            Assert.Equal(HttpStatusCode.NotFound, post.StatusCode);
        }

        using (var post = await client.PostAsJsonAsync($"{BasePath}/itens",
                   new { idProduto = MockProdutoServiceClient.ProdutoIndisponivelId, quantidade = 1 }))
        {
            Assert.Equal(HttpStatusCode.BadRequest, post.StatusCode);
        }
    }

    [Fact]
    public async Task Atualizar_ou_remover_item_ausente_retorna_404()
    {
        using var client = ClientFor(UserA, TenantA);
        var ausente = Guid.NewGuid();

        using (var put = await client.PutAsJsonAsync($"{BasePath}/itens/{ausente}", new { quantidade = 2 }))
        {
            Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
        }

        using (var delete = await client.DeleteAsync($"{BasePath}/itens/{ausente}"))
        {
            Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        }
    }

    [Fact]
    public async Task Usuario_B_nao_ve_carrinho_do_usuario_A()
    {
        using var clientA = ClientFor(UserA, TenantA);
        using var clientB = ClientFor(UserB, TenantA);

        using (var post = await clientA.PostAsJsonAsync($"{BasePath}/itens",
                   new { idProduto = MockProdutoServiceClient.ProdutoDisponivelId, quantidade = 2 }))
        {
            Assert.Equal(HttpStatusCode.OK, post.StatusCode);
        }

        // B ganha carrinho próprio vazio (outro id, total zero).
        string idA, idB;
        using (var getA = await clientA.GetAsync(BasePath))
        {
            idA = (await LerCorpoAsync(getA)).GetProperty("id").GetGuid().ToString();
        }

        using (var getB = await clientB.GetAsync(BasePath))
        {
            Assert.Equal(HttpStatusCode.OK, getB.StatusCode);
            var corpo = await LerCorpoAsync(getB);
            idB = corpo.GetProperty("id").GetGuid().ToString();
            Assert.Equal(0, corpo.GetProperty("valorTotal").GetDecimal());
        }

        Assert.NotEqual(idA, idB);

        // B não altera item de A (no carrinho de B o item não existe).
        using (var put = await clientB.PutAsJsonAsync(
                   $"{BasePath}/itens/{MockProdutoServiceClient.ProdutoDisponivelId}", new { quantidade = 9 }))
        {
            Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
        }

        // Limpeza: esvazia o carrinho de A.
        using var cleanup = ClientFor(UserA, TenantA);
        await cleanup.DeleteAsync($"{BasePath}/itens/{MockProdutoServiceClient.ProdutoDisponivelId}");
    }

    [Fact]
    public async Task Tenants_distintos_tem_carrinhos_isolados()
    {
        using var clientA = ClientFor(UserA, TenantA);
        using var clientC = ClientFor(UserA, TenantC);

        string idA, idC;
        using (var getA = await clientA.GetAsync(BasePath))
        {
            idA = (await LerCorpoAsync(getA)).GetProperty("id").GetGuid().ToString();
        }

        using (var getC = await clientC.GetAsync(BasePath))
        {
            idC = (await LerCorpoAsync(getC)).GetProperty("id").GetGuid().ToString();
        }

        Assert.NotEqual(idA, idC);
    }

    [Fact]
    public async Task Sem_JWT_carrinho_retorna_401()
    {
        using var client = fixture.Factory.CreateClient();

        using var response = await client.GetAsync(BasePath);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
