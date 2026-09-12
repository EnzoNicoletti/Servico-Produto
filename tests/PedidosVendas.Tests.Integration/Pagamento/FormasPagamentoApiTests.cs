using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PedidosVendas.Domain.Entities;
using PedidosVendas.Infrastructure.Persistence;
using PedidosVendas.Infrastructure.Persistence.Seed;
using PedidosVendas.Tests.Integration.Infrastructure;

namespace PedidosVendas.Tests.Integration.Pagamento;

/// <summary>Etapa 06: seed do catálogo, listagem com limites por Tenant e validação.</summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class FormasPagamentoApiTests(IntegrationTestFixture fixture)
{
    private static readonly Guid UserA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TenantA = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private const string BasePath = "/api/v1/formas-pagamento";

    private HttpClient ClientFor(Guid userId, Guid tenantId)
    {
        var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwt.CreateToken(userId, tenantId));
        return client;
    }

    private static Dictionary<string, int> MapaLimites(JsonElement corpo) =>
        corpo.EnumerateArray().ToDictionary(
            e => e.GetProperty("descricao").GetString()!,
            e => e.GetProperty("qtdMaximaParcelas").GetInt32());

    [Fact]
    public async Task Seed_cria_catalogo_e_rerun_nao_duplica()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PedidosVendasDbContext>();

        Assert.Equal(5, await db.FormasPagto.CountAsync());

        await FormaPagtoSeeder.SeedAsync(fixture.Factory.Services);

        Assert.Equal(5, await db.FormasPagto.CountAsync());
    }

    [Fact]
    public async Task Listagem_retorna_catalogo_com_limites_padrao()
    {
        using var client = ClientFor(UserA, TenantA);

        using var response = await client.GetAsync(BasePath);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var limites = MapaLimites(document.RootElement);

        Assert.Equal(5, limites.Count);
        Assert.Equal(1, limites["PIX"]);
        Assert.Equal(1, limites["Transferência"]);
        Assert.Equal(1, limites["Depósito"]);
        Assert.Equal(1, limites["Cartão de Débito"]);
        Assert.Equal(12, limites["Cartão de Crédito"]);
    }

    [Fact]
    public async Task Validar_parcelas_forca_1_no_avista_e_respeita_limite_no_credito()
    {
        using var client = ClientFor(UserA, TenantA);
        using var lista = await client.GetAsync(BasePath);
        using var documento = JsonDocument.Parse(await lista.Content.ReadAsStringAsync());
        var itens = documento.RootElement.EnumerateArray().ToList();
        var pix = itens.First(e => e.GetProperty("descricao").GetString() == "PIX").GetProperty("id").GetGuid();
        var credito = itens.First(e => e.GetProperty("descricao").GetString() == "Cartão de Crédito").GetProperty("id").GetGuid();

        using (var ok = await client.PostAsJsonAsync($"{BasePath}/validar-parcelas",
                   new { idFormaPagto = pix, quantidadeParcelas = 1 }))
        {
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        }

        using (var burla = await client.PostAsJsonAsync($"{BasePath}/validar-parcelas",
                   new { idFormaPagto = pix, quantidadeParcelas = 3 }))
        {
            Assert.Equal(HttpStatusCode.BadRequest, burla.StatusCode);
        }

        using (var ok = await client.PostAsJsonAsync($"{BasePath}/validar-parcelas",
                   new { idFormaPagto = credito, quantidadeParcelas = 12 }))
        {
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        }

        using (var acima = await client.PostAsJsonAsync($"{BasePath}/validar-parcelas",
                   new { idFormaPagto = credito, quantidadeParcelas = 13 }))
        {
            Assert.Equal(HttpStatusCode.BadRequest, acima.StatusCode);
        }

        using (var inexistente = await client.PostAsJsonAsync($"{BasePath}/validar-parcelas",
                   new { idFormaPagto = Guid.NewGuid(), quantidadeParcelas = 1 }))
        {
            Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
        }
    }

    [Fact]
    public async Task Override_do_tenant_altera_listagem_e_validacao()
    {
        using var client = ClientFor(UserA, TenantA);
        Guid creditoId;
        using (var lista = await client.GetAsync(BasePath))
        {
            using var documento = JsonDocument.Parse(await lista.Content.ReadAsStringAsync());
            creditoId = documento.RootElement.EnumerateArray()
                .First(e => e.GetProperty("descricao").GetString() == "Cartão de Crédito")
                .GetProperty("id").GetGuid();
        }

        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PedidosVendasDbContext>();
            db.TenantFormaPagtoConfigs.Add(new TenantFormaPagtoConfig(TenantA, creditoId, 6));
            await db.SaveChangesAsync();
        }

        try
        {
            using (var lista = await client.GetAsync(BasePath))
            {
                using var documento = JsonDocument.Parse(await lista.Content.ReadAsStringAsync());
                Assert.Equal(6, MapaLimites(documento.RootElement)["Cartão de Crédito"]);
            }

            using (var ok = await client.PostAsJsonAsync($"{BasePath}/validar-parcelas",
                       new { idFormaPagto = creditoId, quantidadeParcelas = 6 }))
            {
                Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
            }

            using (var acima = await client.PostAsJsonAsync($"{BasePath}/validar-parcelas",
                       new { idFormaPagto = creditoId, quantidadeParcelas = 7 }))
            {
                Assert.Equal(HttpStatusCode.BadRequest, acima.StatusCode);
            }
        }
        finally
        {
            using var scope = fixture.Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PedidosVendasDbContext>();
            await db.TenantFormaPagtoConfigs
                .Where(c => c.TenantId == TenantA && c.IdFormaPagto == creditoId)
                .ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task Sem_JWT_listagem_retorna_401()
    {
        using var client = fixture.Factory.CreateClient();

        using var response = await client.GetAsync(BasePath);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
