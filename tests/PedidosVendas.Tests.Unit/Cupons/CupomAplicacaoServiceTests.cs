using PedidosVendas.Application.Carrinho;
using PedidosVendas.Application.Cupons;
using PedidosVendas.Domain.Entities;
using PedidosVendas.Infrastructure.Catalog;

namespace PedidosVendas.Tests.Unit.Cupons;

public sealed class CupomAplicacaoServiceTests
{
    private static readonly Guid Tenant = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Cliente = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ProdutoA = MockProdutoServiceClient.ProdutoDisponivelId;
    private static readonly Guid ProdutoB = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly DateTime Hoje = new(2026, 9, 12, 12, 0, 0, DateTimeKind.Utc);

    private readonly PedidoRepositorioFalso _pedidos = new();
    private readonly CupomRepositorioFalso _cupons = new();
    private readonly MockProdutoServiceClient _produtos = new();
    private readonly CupomAplicacaoService _sut;
    private readonly CarrinhoService _carrinho;

    public CupomAplicacaoServiceTests()
    {
        _sut = new CupomAplicacaoService(_pedidos, _cupons, new CupomEligibilityService(), _produtos);
        _carrinho = new CarrinhoService(_pedidos, _produtos);
    }

    private Cupom NovoCupomGlobal(decimal valorDesconto = 30, decimal minimo = 0, int quantidade = 5)
    {
        var cupom = Cupom.Criar(Tenant, "Global", valorDesconto, 0, minimo, null,
            Hoje.AddDays(30), quantidade, false, [], Hoje);
        _cupons.AdicionarDireto(cupom);
        return cupom;
    }

    private Pedido NovoCarrinhoComItens()
    {
        var pedido = Pedido.CriarCarrinho(Tenant, Cliente, null, Hoje);
        pedido.AdicionarItem(ProdutoA, 2, 100); // 200 (categoria do mock)
        pedido.AdicionarItem(ProdutoB, 1, 50); // 50 (fora do catálogo)
        _pedidos.AdicionarDireto(pedido);
        return pedido;
    }

    [Fact]
    public async Task Aplicar_cupom_global_calcula_total_e_rateio()
    {
        var cupom = NovoCupomGlobal(valorDesconto: 30);
        NovoCarrinhoComItens(); // subtotal 250

        var resultado = await _sut.AplicarCupomAsync(Tenant, Cliente, null, cupom.Id, Hoje);

        Assert.NotNull(resultado);
        Assert.Equal(cupom.Id, resultado.Value.Pedido.IdCupom);
        Assert.True(resultado.Value.Desconto.Aplicavel);
        Assert.Equal(30, resultado.Value.Desconto.ValorDesconto);
        // Rateio proporcional: 200/250 e 50/250 de 30.
        Assert.Equal(2, resultado.Value.Desconto.Itens.Count);
        Assert.Equal(30, resultado.Value.Desconto.Itens.Sum(i => i.Desconto));
    }

    [Fact]
    public async Task Aplicar_cupom_de_categoria_alcanca_so_itens_da_categoria()
    {
        var cupom = Cupom.Criar(Tenant, "Categoria", 0, 10, 0,
            MockProdutoServiceClient.CategoriaDoProdutoDisponivelId,
            Hoje.AddDays(30), 5, false, [], Hoje);
        _cupons.AdicionarDireto(cupom);
        NovoCarrinhoComItens();

        var resultado = await _sut.AplicarCupomAsync(Tenant, Cliente, null, cupom.Id, Hoje);

        Assert.NotNull(resultado);
        Assert.True(resultado.Value.Desconto.Aplicavel);
        // 10% só sobre os 200 do produto A (B está fora do catálogo → sem categoria).
        Assert.Equal(20, resultado.Value.Desconto.ValorDesconto);
        Assert.Single(resultado.Value.Desconto.Itens);
    }

    [Fact]
    public async Task Aplicar_cupom_de_produto_alcanca_so_vinculados()
    {
        var cupom = Cupom.Criar(Tenant, "Produto", 0, 50, 0, null,
            Hoje.AddDays(30), 5, true, [ProdutoB], Hoje);
        _cupons.AdicionarDireto(cupom);
        NovoCarrinhoComItens();

        var resultado = await _sut.AplicarCupomAsync(Tenant, Cliente, null, cupom.Id, Hoje);

        Assert.NotNull(resultado);
        // 50% sobre os 50 do produto B (snapshot preservado mesmo fora do catálogo).
        Assert.Equal(25, resultado.Value.Desconto.ValorDesconto);
        Assert.Single(resultado.Value.Desconto.Itens);
    }

    [Fact]
    public async Task Carrinho_que_encolhe_perde_aplicabilidade_mas_mantem_vinculo()
    {
        var cupom = NovoCupomGlobal(valorDesconto: 10, minimo: 200);
        NovoCarrinhoComItens(); // 250: aplicável

        var aplicado = await _sut.AplicarCupomAsync(Tenant, Cliente, null, cupom.Id, Hoje);
        Assert.True(aplicado!.Value.Desconto.Aplicavel);

        // Remove o item de 200: subtotal 50 < mínimo 200.
        await _carrinho.RemoverItemAsync(Tenant, Cliente, null, ProdutoA);

        var resumo = await _sut.CalcularDescontoAsync(Tenant, Cliente, null, Hoje);
        Assert.NotNull(resumo);
        Assert.False(resumo.Aplicavel);
        Assert.Equal(0, resumo.ValorDesconto);
        Assert.Contains("mínimo", resumo.Motivo);

        // Vínculo grudado: recolocar o item reativa sem reaplicar.
        await _carrinho.AdicionarItemAsync(Tenant, Cliente, null, ProdutoA, 2);
        var reativado = await _sut.CalcularDescontoAsync(Tenant, Cliente, null, Hoje);
        Assert.True(reativado!.Aplicavel);

        var apos = await _pedidos.ObterCarrinhoAbertoAsync(Tenant, Cliente, null);
        Assert.Equal(cupom.Id, apos!.IdCupom);
    }

    [Fact]
    public async Task Aplicar_cupom_inexistente_lanca_e_expirado_ou_esgotado_lanca_motivo()
    {
        NovoCarrinhoComItens();

        await Assert.ThrowsAsync<CupomNaoEncontradoException>(
            () => _sut.AplicarCupomAsync(Tenant, Cliente, null, Guid.NewGuid(), Hoje));

        var expirado = Cupom.Criar(Tenant, "Exp", 10, 0, 0, null, Hoje.AddDays(10), 5, false, [], Hoje);
        _cupons.AdicionarDireto(expirado);
        var ex1 = await Assert.ThrowsAsync<CupomInaplicavelException>(
            () => _sut.AplicarCupomAsync(Tenant, Cliente, null, expirado.Id, Hoje.AddDays(11)));
        Assert.Contains("expirado", ex1.Message);

        var esgotado = Cupom.Criar(Tenant, "Esg", 10, 0, 0, null, Hoje.AddDays(10), 0, false, [], Hoje);
        _cupons.AdicionarDireto(esgotado);
        var ex2 = await Assert.ThrowsAsync<CupomInaplicavelException>(
            () => _sut.AplicarCupomAsync(Tenant, Cliente, null, esgotado.Id, Hoje));
        Assert.Contains("esgotado", ex2.Message);
    }

    [Fact]
    public async Task Sem_carrinho_retorna_nulo_e_sem_cupom_resumo_zerado()
    {
        Assert.Null(await _sut.AplicarCupomAsync(Tenant, Cliente, null, Guid.NewGuid(), Hoje));
        Assert.Null(await _sut.CalcularDescontoAsync(Tenant, Cliente, null, Hoje));
        Assert.Null(await _sut.RemoverCupomAsync(Tenant, Cliente, null));

        var pedido = Pedido.CriarCarrinho(Tenant, Cliente, null, Hoje);
        _pedidos.AdicionarDireto(pedido);

        var resumo = await _sut.CalcularDescontoAsync(Tenant, Cliente, null, Hoje);
        Assert.NotNull(resumo);
        Assert.False(resumo.Aplicavel);
        Assert.Contains("Nenhum cupom", resumo.Motivo);
    }
}
