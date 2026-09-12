using PedidosVendas.Application.Cupons;
using PedidosVendas.Domain.Entities;

namespace PedidosVendas.Tests.Unit.Cupons;

public sealed class CupomEligibilityServiceTests
{
    private static readonly Guid Tenant = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid OutroTenant = Guid.Parse("99999999-9999-9999-9999-999999999999");
    private static readonly Guid CategoriaX = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid CategoriaY = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid ProdutoP = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid ProdutoQ = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly DateTime Hoje = new(2026, 9, 12, 12, 0, 0, DateTimeKind.Utc);

    private readonly CupomEligibilityService _sut = new();

    private static Cupom NovoCupom(
        Guid? idCategoria = null,
        IReadOnlyList<Guid>? idsProdutos = null,
        decimal valorDesconto = 10,
        decimal percDesconto = 0,
        decimal minimo = 0,
        int quantidade = 5,
        DateTime? validade = null) =>
        Cupom.Criar(
            Tenant, "Teste", valorDesconto, percDesconto, minimo, idCategoria,
            validade ?? Hoje.AddDays(30), quantidade,
            idsProdutos?.Count > 0, idsProdutos ?? [],
            agoraUtc: Hoje);

    private static List<CarrinhoItem> Carrinho() =>
    [
        new(ProdutoP, CategoriaX, 1, 100),
        new(ProdutoQ, CategoriaY, 2, 50),
    ];

    [Fact]
    public void Cupom_global_elegivel_para_todos_os_itens()
    {
        var elegiveis = _sut.ObterItensElegiveis(NovoCupom(), Carrinho());

        Assert.Equal(2, elegiveis.Count);
    }

    [Fact]
    public void Cupom_de_categoria_elegivel_apenas_para_itens_da_categoria()
    {
        var elegiveis = _sut.ObterItensElegiveis(NovoCupom(idCategoria: CategoriaX), Carrinho());

        Assert.Single(elegiveis);
        Assert.Equal(ProdutoP, elegiveis[0].IdProduto);
    }

    [Fact]
    public void Cupom_de_categoria_ignora_item_sem_categoria()
    {
        var itens = new List<CarrinhoItem> { new(Guid.NewGuid(), null, 1, 10) };

        Assert.Empty(_sut.ObterItensElegiveis(NovoCupom(idCategoria: CategoriaX), itens));
    }

    [Fact]
    public void Produto_especifico_tem_prioridade_sobre_categoria()
    {
        // Cupom com vínculo ao produto P (categoria Y) E categoria X:
        // P é elegível pelo vínculo; Q (categoria X, sem vínculo) não é.
        var cupom = NovoCupom(idCategoria: CategoriaX, idsProdutos: [ProdutoP]);
        var itens = new List<CarrinhoItem>
        {
            new(ProdutoP, CategoriaY, 1, 100),
            new(ProdutoQ, CategoriaX, 1, 100),
        };

        var elegiveis = _sut.ObterItensElegiveis(cupom, itens);

        Assert.Single(elegiveis);
        Assert.Equal(ProdutoP, elegiveis[0].IdProduto);
    }

    [Fact]
    public void Cupom_com_vinculos_mas_carrinho_sem_vinculados_retorna_vazio()
    {
        var cupom = NovoCupom(idsProdutos: [Guid.NewGuid()]);

        Assert.Empty(_sut.ObterItensElegiveis(cupom, Carrinho()));
    }

    [Fact]
    public void Cupom_sem_nenhum_vinculo_nem_categoria_e_global()
    {
        var cupom = NovoCupom();

        Assert.Equal(2, _sut.ObterItensElegiveis(cupom, Carrinho()).Count);
    }

    [Theory]
    [InlineData(200, true)] // acima do mínimo
    [InlineData(150, true)] // igual ao mínimo
    [InlineData(149.99, false)] // abaixo do mínimo
    public void PodeAplicar_respeita_valor_minimo(decimal subtotal, bool esperado)
    {
        var cupom = NovoCupom(minimo: 150);

        Assert.Equal(esperado, _sut.PodeAplicar(cupom, Tenant, subtotal, Hoje));
    }

    [Fact]
    public void PodeAplicar_rejeita_tenant_divergente_expirado_e_esgotado()
    {
        var cupom = NovoCupom();

        Assert.False(_sut.PodeAplicar(cupom, OutroTenant, 1000, Hoje));
        Assert.False(_sut.PodeAplicar(cupom, Tenant, 1000, Hoje.AddDays(31)));
        Assert.False(_sut.PodeAplicar(NovoCupom(quantidade: 0), Tenant, 1000, Hoje));
    }

    [Fact]
    public void CalcularDesconto_fixo_limitado_ao_subtotal_elegivel()
    {
        var cupom = NovoCupom(valorDesconto: 500); // maior que o subtotal (200)

        Assert.Equal(200, _sut.CalcularDesconto(cupom, Carrinho()));
    }

    [Fact]
    public void CalcularDesconto_percentual_apenas_sobre_elegiveis()
    {
        var cupom = NovoCupom(valorDesconto: 0, percDesconto: 10, idCategoria: CategoriaX);

        // 10% de 100 (só o item da categoria X).
        Assert.Equal(10, _sut.CalcularDesconto(cupom, _sut.ObterItensElegiveis(cupom, Carrinho())));
    }

    [Fact]
    public void CalcularDesconto_sem_itens_retorna_zero()
    {
        Assert.Equal(0, _sut.CalcularDesconto(NovoCupom(), []));
    }
}
