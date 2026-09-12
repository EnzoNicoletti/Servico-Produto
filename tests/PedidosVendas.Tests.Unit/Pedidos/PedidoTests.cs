using PedidosVendas.Domain.Entities;
using PedidosVendas.Domain.Enums;
using PedidosVendas.Domain.Exceptions;

namespace PedidosVendas.Tests.Unit.Pedidos;

public sealed class PedidoTests
{
    private static readonly Guid Tenant = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Cliente = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Unidade = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ProdutoA = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid ProdutoB = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly DateTime Hoje = new(2026, 9, 12, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CriarCarrinho_nasce_aberto_com_total_zero()
    {
        var pedido = Pedido.CriarCarrinho(Tenant, Cliente, Unidade, Hoje);

        Assert.NotEqual(Guid.Empty, pedido.Id);
        Assert.Equal(StatusPedido.Carrinho, pedido.Status);
        Assert.Equal(0, pedido.ValorTotal);
        Assert.Equal(Hoje, pedido.DataAbertura);
        Assert.Equal(Cliente, pedido.IdCliente);
        Assert.Equal(Unidade, pedido.IdUnidade);
        Assert.Null(pedido.IdCupom);
        Assert.Null(pedido.DataFechamento);
        Assert.Empty(pedido.Itens);
    }

    [Fact]
    public void CriarCarrinho_rejeita_tenant_vazio()
    {
        Assert.Throws<PedidoInvalidoException>(
            () => Pedido.CriarCarrinho(Guid.Empty, Cliente, Unidade, Hoje));
    }

    [Fact]
    public void AdicionarItem_soma_total()
    {
        var pedido = Pedido.CriarCarrinho(Tenant, Cliente, Unidade, Hoje);

        pedido.AdicionarItem(ProdutoA, 2, 99.90m);

        Assert.Single(pedido.Itens);
        Assert.Equal(199.80m, pedido.ValorTotal);
    }

    [Fact]
    public void Adicionar_mesmo_produto_soma_quantidade_sem_duplicar_linha()
    {
        var pedido = Pedido.CriarCarrinho(Tenant, Cliente, Unidade, Hoje);

        pedido.AdicionarItem(ProdutoA, 2, 100);
        pedido.AdicionarItem(ProdutoA, 1, 100);

        Assert.Single(pedido.Itens);
        Assert.Equal(3, pedido.Itens.First().Quantidade);
        Assert.Equal(300, pedido.ValorTotal);
    }

    [Fact]
    public void DefinirQuantidade_recalcula_total()
    {
        var pedido = Pedido.CriarCarrinho(Tenant, Cliente, Unidade, Hoje);
        pedido.AdicionarItem(ProdutoA, 2, 100);
        pedido.AdicionarItem(ProdutoB, 1, 50);

        pedido.DefinirQuantidade(ProdutoA, 1);

        Assert.Equal(150, pedido.ValorTotal);
    }

    [Fact]
    public void RemoverItem_recalcula_total()
    {
        var pedido = Pedido.CriarCarrinho(Tenant, Cliente, Unidade, Hoje);
        pedido.AdicionarItem(ProdutoA, 2, 100);
        pedido.AdicionarItem(ProdutoB, 1, 50);

        pedido.RemoverItem(ProdutoA);

        Assert.Single(pedido.Itens);
        Assert.Equal(50, pedido.ValorTotal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AdicionarItem_rejeita_quantidade_nao_positiva(int quantidade)
    {
        var pedido = Pedido.CriarCarrinho(Tenant, Cliente, Unidade, Hoje);

        Assert.Throws<PedidoInvalidoException>(
            () => pedido.AdicionarItem(ProdutoA, quantidade, 100));
    }

    [Fact]
    public void AdicionarItem_rejeita_valor_negativo_e_ids_vazios()
    {
        var pedido = Pedido.CriarCarrinho(Tenant, Cliente, Unidade, Hoje);

        Assert.Throws<PedidoInvalidoException>(
            () => pedido.AdicionarItem(ProdutoA, 1, -1));
        Assert.Throws<PedidoInvalidoException>(
            () => pedido.AdicionarItem(Guid.Empty, 1, 100));
    }

    [Fact]
    public void Definir_e_remover_item_ausente_lancam()
    {
        var pedido = Pedido.CriarCarrinho(Tenant, Cliente, Unidade, Hoje);

        Assert.Throws<PedidoInvalidoException>(
            () => pedido.DefinirQuantidade(ProdutoA, 2));
        Assert.Throws<PedidoInvalidoException>(
            () => pedido.RemoverItem(ProdutoA));
        Assert.Throws<PedidoInvalidoException>(
            () => pedido.DefinirQuantidade(ProdutoA, 0));
    }
}
