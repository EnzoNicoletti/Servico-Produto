namespace PedidosVendas.Application.Frete;

/// <summary>
/// Item para cotação de frete (preço do snapshot do carrinho; peso/volume entram aqui
/// quando o catálogo expuser — sem tocar no domínio).
/// </summary>
public sealed record ItemFrete(
    Guid IdProduto,
    int Quantidade,
    decimal ValorUnitario);
