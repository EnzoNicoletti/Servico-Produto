namespace PedidosVendas.Application.Catalog;

/// <summary>
/// Snapshot mínimo de produto do serviço de Estoque.
/// Etapa 04: `IdCategoria` (referência viva, não snapshot) permite avaliar cupons por
/// categoria sobre itens do carrinho — `ProdutosPedido` guarda só o preço (decisão 4.8),
/// e a categoria é resolvida em tempo de cálculo. Aditivo e retrocompatível com o mock.
/// </summary>
public sealed record ProdutoDto(
    Guid Id,
    string Nome,
    decimal Preco,
    bool Disponivel,
    Guid? IdCategoria = null);
