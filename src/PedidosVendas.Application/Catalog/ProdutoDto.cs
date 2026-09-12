namespace PedidosVendas.Application.Catalog;

/// <summary>
/// Snapshot mínimo de produto do serviço de Estoque necessário ao carrinho
/// (Etapa 03). Mantido intencionalmente enxuto nesta etapa.
/// </summary>
public sealed record ProdutoDto(
    Guid Id,
    string Nome,
    decimal Preco,
    bool Disponivel);
