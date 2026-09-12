using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PedidosVendas.Domain.Interfaces;
using PedidosVendas.Tests.Integration.Infrastructure;

namespace PedidosVendas.Tests.Integration.Cupons;

/// <summary>
/// Etapa 02: CRUD de Cupom tenant-scoped, isolamento de Tenant, constraint de banco
/// e concorrência otimista — contra PostgreSQL real (Testcontainers).
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class CuponsApiTests(IntegrationTestFixture fixture)
{
    private static readonly Guid UserA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TenantA = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid UserB = Guid.Parse("77777777-7777-7777-7777-777777777777");
    private static readonly Guid TenantB = Guid.Parse("88888888-8888-8888-8888-888888888888");

    private const string BasePath = "/api/v1/cupons";

    private HttpClient ClientFor(Guid userId, Guid tenantId)
    {
        var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwt.CreateToken(userId, tenantId));
        return client;
    }

    private static object NovoCupom(
        string descricao = "Cupom teste",
        decimal valorDesconto = 10,
        decimal percDesconto = 0,
        bool isCupomProduto = false,
        Guid[]? idsProdutos = null,
        int quantidade = 5,
        DateTime? validade = null) => new
        {
            descricao,
            valorDesconto,
            percDesconto,
            valorMinimoCompra = 0,
            idCategoria = (Guid?)null,
            dataValidade = (validade ?? DateTime.UtcNow.AddDays(30)).ToString("O"),
            quantidade,
            isCupomProduto,
            idsProdutos = idsProdutos ?? []
        };

    private static async Task<Guid> CriarCupomAsync(HttpClient client, object body)
    {
        using var response = await client.PostAsJsonAsync(BasePath, body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Crud_completo_retorna_201_200_204_e_404_final()
    {
        using var client = ClientFor(UserA, TenantA);
        var produto = Guid.NewGuid();

        var id = await CriarCupomAsync(client, NovoCupom("Black Friday", percDesconto: 0, valorDesconto: 25));

        using (var get = await client.GetAsync($"{BasePath}/{id}"))
        {
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            using var document = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
            Assert.Equal("Black Friday", document.RootElement.GetProperty("descricao").GetString());
        }

        using (var put = await client.PutAsJsonAsync($"{BasePath}/{id}",
                   NovoCupom("Black Friday Plus", valorDesconto: 0, percDesconto: 10, isCupomProduto: true, idsProdutos: [produto])))
        {
            Assert.Equal(HttpStatusCode.OK, put.StatusCode);
            using var document = JsonDocument.Parse(await put.Content.ReadAsStringAsync());
            Assert.Equal(10, document.RootElement.GetProperty("percDesconto").GetDecimal());
            Assert.Contains(produto, document.RootElement.GetProperty("idsProdutos").EnumerateArray().Select(e => e.GetGuid()));
        }

        // Atualizar de volta para global remove os vínculos (deleção de órfãos).
        using (var put2 = await client.PutAsJsonAsync($"{BasePath}/{id}", NovoCupom("Global de novo")))
        {
            Assert.Equal(HttpStatusCode.OK, put2.StatusCode);
            using var document = JsonDocument.Parse(await put2.Content.ReadAsStringAsync());
            Assert.Empty(document.RootElement.GetProperty("idsProdutos").EnumerateArray());
        }

        using (var delete = await client.DeleteAsync($"{BasePath}/{id}"))
        {
            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        }

        using (var getFinal = await client.GetAsync($"{BasePath}/{id}"))
        {
            Assert.Equal(HttpStatusCode.NotFound, getFinal.StatusCode);
        }
    }

    [Fact]
    public async Task Tenant_B_nao_enxerga_nem_altera_cupom_do_Tenant_A()
    {
        using var clientA = ClientFor(UserA, TenantA);
        using var clientB = ClientFor(UserB, TenantB);

        var id = await CriarCupomAsync(clientA, NovoCupom("Exclusivo A"));

        using (var get = await clientB.GetAsync($"{BasePath}/{id}"))
        {
            Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        }

        using (var put = await clientB.PutAsJsonAsync($"{BasePath}/{id}", NovoCupom("Tentativa B")))
        {
            Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
        }

        using (var delete = await clientB.DeleteAsync($"{BasePath}/{id}"))
        {
            Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        }

        using (var list = await clientB.GetAsync($"{BasePath}?pagina=1&tamanhoPagina=20"))
        {
            using var document = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
            var itens = document.RootElement.GetProperty("itens").EnumerateArray();
            Assert.DoesNotContain(itens, e => e.GetProperty("id").GetGuid() == id);
        }

        using (var cleanup = await clientA.DeleteAsync($"{BasePath}/{id}"))
        {
            Assert.Equal(HttpStatusCode.NoContent, cleanup.StatusCode);
        }
    }

    [Fact]
    public async Task Listagem_paginada_retorna_total_e_fatias()
    {
        using var client = ClientFor(UserA, TenantA);
        var sufixo = Guid.NewGuid().ToString("N")[..8];

        var ids = new List<Guid>();
        for (var i = 0; i < 3; i++)
        {
            ids.Add(await CriarCupomAsync(client, NovoCupom($"Pag-{sufixo}-{i}")));
        }

        try
        {
            using var page1 = await client.GetAsync($"{BasePath}?pagina=1&tamanhoPagina=2");
            using var doc1 = JsonDocument.Parse(await page1.Content.ReadAsStringAsync());
            Assert.True(doc1.RootElement.GetProperty("total").GetInt32() >= 3);
            Assert.Equal(2, doc1.RootElement.GetProperty("itens").EnumerateArray().Count());

            using var page2 = await client.GetAsync($"{BasePath}?pagina=2&tamanhoPagina=2");
            using var doc2 = JsonDocument.Parse(await page2.Content.ReadAsStringAsync());
            Assert.True(doc2.RootElement.GetProperty("itens").EnumerateArray().Count() >= 1);
        }
        finally
        {
            foreach (var id in ids)
            {
                await client.DeleteAsync($"{BasePath}/{id}");
            }
        }
    }

    [Fact]
    public async Task Criar_com_dois_descontos_ou_validade_passada_retorna_400()
    {
        using var client = ClientFor(UserA, TenantA);

        using (var ambos = await client.PostAsJsonAsync(BasePath, NovoCupom(valorDesconto: 10, percDesconto: 5)))
        {
            Assert.Equal(HttpStatusCode.BadRequest, ambos.StatusCode);
        }

        using (var passada = await client.PostAsJsonAsync(
                   BasePath, NovoCupom(validade: DateTime.UtcNow.AddDays(-1))))
        {
            Assert.Equal(HttpStatusCode.BadRequest, passada.StatusCode);
        }
    }

    [Fact]
    public async Task Check_constraint_impede_dois_descontos_no_banco()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PedidosVendas.Infrastructure.Persistence.PedidosVendasDbContext>();

        // SQL direto não passa pelo pipeline do EF: o PostgreSQL devolve o erro puro.
        var categoria = Guid.NewGuid();
        var exception = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlAsync(
            $"INSERT INTO \"Cupom\" (\"Id\", \"TenantId\", \"Descricao\", \"ValorDesconto\", \"PercDesconto\", \"ValorMinimoCompra\", \"IdCategoria\", \"DataValidade\", \"DataCriacao\", \"Quantidade\", \"IsCupomProduto\") VALUES ({Guid.NewGuid()}, {TenantA}, {"Burla"}, {10m}, {5m}, {0m}, {categoria}, {DateTime.UtcNow.AddDays(10)}, {DateTime.UtcNow}, {3}, {false})"));

        Assert.Equal("23514", exception.SqlState); // check_violation em CK_Cupom_DescontoExclusivo
    }

    [Fact]
    public async Task Criar_com_vinculos_persiste_links()
    {
        using var client = ClientFor(UserA, TenantA);
        var produto = Guid.NewGuid();

        var id = await CriarCupomAsync(client, NovoCupom(
            "Com links", valorDesconto: 0, percDesconto: 5, isCupomProduto: true, idsProdutos: [produto]));

        try
        {
            using var get = await client.GetAsync($"{BasePath}/{id}");
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            using var document = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
            Assert.Contains(produto, document.RootElement.GetProperty("idsProdutos").EnumerateArray().Select(e => e.GetGuid()));
        }
        finally
        {
            await client.DeleteAsync($"{BasePath}/{id}");
        }
    }

    [Fact]
    public async Task Dois_consumos_concorrentes_da_ultima_unidade_apenas_um_vence()
    {
        using var client = ClientFor(UserA, TenantA);
        var id = await CriarCupomAsync(client, NovoCupom(quantidade: 1));

        try
        {
            using var scopeA = fixture.Factory.Services.CreateScope();
            using var scopeB = fixture.Factory.Services.CreateScope();
            var repoA = scopeA.ServiceProvider.GetRequiredService<ICupomRepository>();
            var repoB = scopeB.ServiceProvider.GetRequiredService<ICupomRepository>();

            var cupomA = await repoA.ObterParaAtualizacaoAsync(id, TenantA);
            var cupomB = await repoB.ObterParaAtualizacaoAsync(id, TenantA);
            Assert.NotNull(cupomA);
            Assert.NotNull(cupomB);

            cupomA.RegistrarUso();
            await repoA.AtualizarAsync(cupomA);

            cupomB.RegistrarUso();
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => repoB.AtualizarAsync(cupomB));
        }
        finally
        {
            await client.DeleteAsync($"{BasePath}/{id}");
        }
    }
}
