namespace PedidosVendas.Application.Carrinho;

/// <summary>Produto indisponível no Estoque (API traduz em 400 com mensagem clara).</summary>
public sealed class ProdutoIndisponivelException(Guid idProduto)
    : Exception($"Produto {idProduto} indisponível no Estoque.");
