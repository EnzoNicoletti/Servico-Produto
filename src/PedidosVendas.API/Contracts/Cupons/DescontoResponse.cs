using PedidosVendas.Application.Cupons;

namespace PedidosVendas.API.Contracts.Cupons;

public sealed record ItemDescontoResponse(
    Guid IdProduto,
    int Quantidade,
    decimal ValorUnitario,
    decimal Desconto);

public sealed record DescontoResponse(
    bool Aplicavel,
    string? Motivo,
    decimal ValorDesconto,
    decimal SubtotalElegivel,
    IReadOnlyList<ItemDescontoResponse> Itens)
{
    public static DescontoResponse From(ResumoDesconto resumo) => new(
        resumo.Aplicavel,
        resumo.Motivo,
        resumo.ValorDesconto,
        resumo.SubtotalElegivel,
        resumo.Itens
            .Select(i => new ItemDescontoResponse(i.IdProduto, i.Quantidade, i.ValorUnitario, i.Desconto))
            .ToList());
}

public sealed record CarrinhoComDescontoResponse(
    Carrinho.CarrinhoResponse Carrinho,
    DescontoResponse Desconto);
