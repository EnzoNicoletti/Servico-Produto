using PedidosVendas.Domain.Entities;

namespace PedidosVendas.API.Contracts.Cupons;

public sealed record CupomResponse(
    Guid Id,
    string Descricao,
    decimal ValorDesconto,
    decimal PercDesconto,
    decimal ValorMinimoCompra,
    Guid? IdCategoria,
    DateTime DataValidade,
    DateTime DataCriacao,
    int Quantidade,
    bool IsCupomProduto,
    IReadOnlyList<Guid> IdsProdutos)
{
    public static CupomResponse From(Cupom cupom) => new(
        cupom.Id,
        cupom.Descricao,
        cupom.ValorDesconto,
        cupom.PercDesconto,
        cupom.ValorMinimoCompra,
        cupom.IdCategoria,
        cupom.DataValidade,
        cupom.DataCriacao,
        cupom.Quantidade,
        cupom.IsCupomProduto,
        cupom.CupomProdutos.Select(cp => cp.IdProduto).ToList());
}
