namespace PedidosVendas.Application.Carrinho;

/// <summary>Produto inexistente no Estoque (API traduz em 404 com mensagem clara).</summary>
public sealed class ProdutoNaoEncontradoException(Guid idProduto)
    : Exception($"Produto {idProduto} não encontrado no Estoque.");
