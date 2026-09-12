using PedidosVendas.Domain.Entities;

namespace PedidosVendas.API.Contracts.Carrinho;

public sealed record ItemCarrinhoResponse(
    Guid IdProduto,
    int Quantidade,
    decimal ValorUnitario,
    decimal Subtotal);

public sealed record CarrinhoResponse(
    Guid Id,
    string Status,
    decimal ValorTotal,
    DateTime DataAbertura,
    Guid? IdUnidade,
    IReadOnlyList<ItemCarrinhoResponse> Itens)
{
    public static CarrinhoResponse From(Pedido pedido) => new(
        pedido.Id,
        pedido.Status.ToString(),
        pedido.ValorTotal,
        pedido.DataAbertura,
        pedido.IdUnidade,
        pedido.Itens
            .Select(i => new ItemCarrinhoResponse(i.IdProduto, i.Quantidade, i.ValorUnitario, i.Subtotal))
            .ToList());
}
