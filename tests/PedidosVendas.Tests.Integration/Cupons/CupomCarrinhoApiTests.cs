using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PedidosVendas.Application.Cupons;
using PedidosVendas.Infrastructure.Catalog;
using PedidosVendas.Tests.Integration.Infrastructure;

namespace PedidosVendas.Tests.Integration.Cupons;

/// <summary>
/// Etapa 04: aplicar/remover cupom e consultar desconto via API + concorrência no consumo.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class CupomCarrinhoApiTests(IntegrationTestFixture fixture)
{
    private static readonly Guid UserA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TenantA = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid UserB = Guid.Parse("77777777-7777-7777-7777-777777777777");
    private static readonly Guid TenantB = Guid.Parse("88888888-8888-8888-8888-888888888888");

    private const string CarrinhoPath = "/api/v1/carrinho";
    private const string CuponsPath = "/api/v1/cupons";

    private HttpClient ClientFor(Guid userId, Guid tenantId)
    {
        var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwt.CreateToken(userId, tenantId));
        return client;
    }

    private static async Task<Guid> CriarCupomAsync(HttpClient client, object body)
    {
        using var response = await client.PostAsJsonAsync(CuponsPath, body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("id").GetGuid();
    }

    private static object NovoCupom(
        string descricao = "Cupom", decimal valorDesconto = 25, decimal minimo = 100,
        int quantidade = 5, Guid? idCategoria = null) => new
        {
            descricao,
            valorDesconto,
            percDesconto = 0,
            valorMinimoCompra = minimo,
            idCategoria,
            dataValidade = DateTime.UtcNow.AddDays(30).ToString("O"),
            quantidade,
            isCupomProduto = false,
            idsProdutos = Array.Empty<Guid>()
        };

    private static async Task AdicionarItemAsync(HttpClient client, Guid produto, int quantidade)
    {
        using var response = await client.PostAsJsonAsync($"{CarrinhoPath}/itens",
            new { idProduto = produto, quantidade });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Aplicar_e_remover_cupom_recalcula_desconto_sem_decrementar_quantidade()
    {
        using var client = ClientFor(UserA, TenantA);
        var cupomId = await CriarCupomAsync(client, NovoCupom());
        await AdicionarItemAsync(client, MockProdutoServiceClient.ProdutoDisponivelId, 2); // 199.80

        try
        {
            Guid pedidoId;
            using (var aplicar = await client.PostAsJsonAsync($"{CarrinhoPath}/cupom", new { idCupom = cupomId }))
            {
                Assert.Equal(HttpStatusCode.OK, aplicar.StatusCode);
                using var document = JsonDocument.Parse(await aplicar.Content.ReadAsStringAsync());
                pedidoId = document.RootElement.GetProperty("carrinho").GetProperty("id").GetGuid();
                var desconto = document.RootElement.GetProperty("desconto");
                Assert.True(desconto.GetProperty("aplicavel").GetBoolean());
                Assert.Equal(25, desconto.GetProperty("valorDesconto").GetDecimal());
                // Total do pedido intacto (desconto exibido à parte).
                Assert.Equal(199.80m, document.RootElement.GetProperty("carrinho").GetProperty("valorTotal").GetDecimal());
            }

            // Quantidade NÃO decrementada ao aplicar (só na finalização).
            using (var get = await client.GetAsync($"{CuponsPath}/{cupomId}"))
            {
                using var document = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
                Assert.Equal(5, document.RootElement.GetProperty("quantidade").GetInt32());
            }

            // GET desconto reflete o mesmo valor.
            using (var desconto = await client.GetAsync($"{CarrinhoPath}/desconto"))
            {
                using var document = JsonDocument.Parse(await desconto.Content.ReadAsStringAsync());
                Assert.True(document.RootElement.GetProperty("aplicavel").GetBoolean());
                Assert.Equal(25, document.RootElement.GetProperty("valorDesconto").GetDecimal());
            }

            // Remover cupom zera o desconto.
            using (var remover = await client.DeleteAsync($"{CarrinhoPath}/cupom"))
            {
                Assert.Equal(HttpStatusCode.OK, remover.StatusCode);
            }

            using (var desconto = await client.GetAsync($"{CarrinhoPath}/desconto"))
            {
                using var document = JsonDocument.Parse(await desconto.Content.ReadAsStringAsync());
                Assert.False(document.RootElement.GetProperty("aplicavel").GetBoolean());
                Assert.Equal(0, document.RootElement.GetProperty("valorDesconto").GetDecimal());
            }

            // Limpeza.
            await client.DeleteAsync($"{CarrinhoPath}/itens/{MockProdutoServiceClient.ProdutoDisponivelId}");
            await client.DeleteAsync($"{CuponsPath}/{cupomId}");
            Assert.NotEqual(Guid.Empty, pedidoId);
        }
        catch
        {
            await client.DeleteAsync($"{CarrinhoPath}/itens/{MockProdutoServiceClient.ProdutoDisponivelId}");
            await client.DeleteAsync($"{CuponsPath}/{cupomId}");
            throw;
        }
    }

    [Fact]
    public async Task Carrinho_que_encolhe_mantem_cupom_mas_desconto_fica_inaplicavel()
    {
        using var client = ClientFor(UserA, TenantA);
        var cupomId = await CriarCupomAsync(client, NovoCupom(minimo: 150));
        await AdicionarItemAsync(client, MockProdutoServiceClient.ProdutoDisponivelId, 2); // 199.80

        try
        {
            using (var aplicar = await client.PostAsJsonAsync($"{CarrinhoPath}/cupom", new { idCupom = cupomId }))
            {
                Assert.Equal(HttpStatusCode.OK, aplicar.StatusCode);
            }

            // Remove 1 unidade: total 99.90 < mínimo 150.
            using (var put = await client.PutAsJsonAsync(
                       $"{CarrinhoPath}/itens/{MockProdutoServiceClient.ProdutoDisponivelId}", new { quantidade = 1 }))
            {
                Assert.Equal(HttpStatusCode.OK, put.StatusCode);
            }

            using (var desconto = await client.GetAsync($"{CarrinhoPath}/desconto"))
            {
                using var document = JsonDocument.Parse(await desconto.Content.ReadAsStringAsync());
                Assert.False(document.RootElement.GetProperty("aplicavel").GetBoolean());
                Assert.Equal(0, document.RootElement.GetProperty("valorDesconto").GetDecimal());
                Assert.Contains("mínimo", document.RootElement.GetProperty("motivo").GetString());
            }
        }
        finally
        {
            await client.DeleteAsync($"{CarrinhoPath}/itens/{MockProdutoServiceClient.ProdutoDisponivelId}");
            await client.DeleteAsync($"{CuponsPath}/{cupomId}");
        }
    }

    [Fact]
    public async Task Aplicar_cupom_de_outro_tenant_ou_inexistente_retorna_404_e_invalido_400()
    {
        using var clientA = ClientFor(UserA, TenantA);
        using var clientB = ClientFor(UserB, TenantB);
        var cupomId = await CriarCupomAsync(clientA, NovoCupom());
        await AdicionarItemAsync(clientB, MockProdutoServiceClient.ProdutoDisponivelId, 2);

        try
        {
            // Tenant B não enxerga o cupom de A.
            using (var aplicar = await clientB.PostAsJsonAsync($"{CarrinhoPath}/cupom", new { idCupom = cupomId }))
            {
                Assert.Equal(HttpStatusCode.NotFound, aplicar.StatusCode);
            }

            // Id inexistente.
            using (var aplicar = await clientB.PostAsJsonAsync($"{CarrinhoPath}/cupom", new { idCupom = Guid.NewGuid() }))
            {
                Assert.Equal(HttpStatusCode.NotFound, aplicar.StatusCode);
            }

            // Cupom esgotado (quantidade 0) → 400 com motivo.
            var esgotadoId = await CriarCupomAsync(clientB, NovoCupom("Esgotado", quantidade: 0));
            try
            {
                using var aplicar = await clientB.PostAsJsonAsync($"{CarrinhoPath}/cupom", new { idCupom = esgotadoId });
                Assert.Equal(HttpStatusCode.BadRequest, aplicar.StatusCode);
            }
            finally
            {
                await clientB.DeleteAsync($"{CuponsPath}/{esgotadoId}");
            }

            // Sem carrinho (usuário sem itens e sem GET prévio... B tem carrinho; usa tenant novo).
            using var clientNovo = ClientFor(Guid.NewGuid(), Guid.NewGuid());
            using (var aplicar = await clientNovo.PostAsJsonAsync($"{CarrinhoPath}/cupom", new { idCupom = cupomId }))
            {
                Assert.Equal(HttpStatusCode.NotFound, aplicar.StatusCode);
            }
        }
        finally
        {
            await clientB.DeleteAsync($"{CarrinhoPath}/itens/{MockProdutoServiceClient.ProdutoDisponivelId}");
            await clientA.DeleteAsync($"{CuponsPath}/{cupomId}");
        }
    }

    [Fact]
    public async Task Duas_finalizacoes_simuladas_apenas_uma_consome_a_ultima_unidade()
    {
        using var client = ClientFor(UserA, TenantA);
        var cupomId = await CriarCupomAsync(client, NovoCupom(quantidade: 1));

        try
        {
            using var scopeA = fixture.Factory.Services.CreateScope();
            using var scopeB = fixture.Factory.Services.CreateScope();
            var servicoA = scopeA.ServiceProvider.GetRequiredService<ICupomService>();
            var servicoB = scopeB.ServiceProvider.GetRequiredService<ICupomService>();

            var resultados = await Task.WhenAll(
                TentarConsumirAsync(servicoA, cupomId),
                TentarConsumirAsync(servicoB, cupomId));

            // Exatamente um sucesso. O perdedor falha de forma controlada (erro de
            // negócio claro, sem vazar exceção de infra): "esgotado", se leu após o
            // vencedor gravar, ou "outra operação", se houve conflito otimista (xmin).
            // Qual ramo ocorre depende do escalonamento — ambos são controlados.
            Assert.Single(resultados, r => r.Sucesso);
            var falha = Assert.Single(resultados, r => !r.Sucesso);
            Assert.False(string.IsNullOrWhiteSpace(falha.Erro));
        }
        finally
        {
            await client.DeleteAsync($"{CuponsPath}/{cupomId}");
        }

        async Task<(bool Sucesso, string? Erro)> TentarConsumirAsync(ICupomService servico, Guid id)
        {
            try
            {
                await servico.ConsumirAsync(id, TenantA);
                return (true, null);
            }
            catch (Domain.Exceptions.CupomInvalidoException exception)
            {
                return (false, exception.Message);
            }
        }
    }
}
