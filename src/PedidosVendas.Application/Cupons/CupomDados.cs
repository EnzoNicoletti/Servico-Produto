namespace PedidosVendas.Application.Cupons;

/// <summary>
/// Dados de entrada para criar/atualizar um cupom (sem TenantId: sempre do contexto).
/// </summary>
public sealed record CupomDados(
    string Descricao,
    decimal ValorDesconto,
    decimal PercDesconto,
    decimal ValorMinimoCompra,
    Guid? IdCategoria,
    DateTime DataValidade,
    int Quantidade,
    bool IsCupomProduto,
    IReadOnlyList<Guid> IdsProdutos);
