using PedidosVendas.Application.Catalog;
using PedidosVendas.Domain.Entities;
using PedidosVendas.Domain.Interfaces;

namespace PedidosVendas.Application.Carrinho;

public sealed class CarrinhoService(
    IPedidoRepository pedidos,
    IProdutoServiceClient produtos) : ICarrinhoService
{
    public async Task<Pedido> ObterOuCriarAsync(
        Guid tenantId, Guid? idCliente, Guid? idUnidade, CancellationToken cancellationToken = default)
    {
        var carrinho = await pedidos.ObterCarrinhoAbertoAsync(tenantId, idCliente, idUnidade, cancellationToken);
        if (carrinho is not null)
        {
            return carrinho;
        }

        carrinho = Pedido.CriarCarrinho(tenantId, idCliente, idUnidade);
        await pedidos.AdicionarAsync(carrinho, cancellationToken);
        return carrinho;
    }

    public async Task<Pedido> AdicionarItemAsync(
        Guid tenantId, Guid? idCliente, Guid? idUnidade,
        Guid idProduto, int quantidade, CancellationToken cancellationToken = default)
    {
        var produto = await produtos.ObterPorIdAsync(idProduto, cancellationToken)
            ?? throw new ProdutoNaoEncontradoException(idProduto);

        if (!produto.Disponivel)
        {
            throw new ProdutoIndisponivelException(idProduto);
        }

        var carrinho = await ObterOuCriarAsync(tenantId, idCliente, idUnidade, cancellationToken);
        carrinho.AdicionarItem(produto.Id, quantidade, produto.Preco);
        await pedidos.SalvarAsync(carrinho, cancellationToken);
        return carrinho;
    }

    public async Task<Pedido?> DefinirQuantidadeAsync(
        Guid tenantId, Guid? idCliente, Guid? idUnidade,
        Guid idProduto, int novaQuantidade, CancellationToken cancellationToken = default)
    {
        var carrinho = await pedidos.ObterCarrinhoAbertoAsync(tenantId, idCliente, idUnidade, cancellationToken);
        if (carrinho is null || carrinho.Itens.All(i => i.IdProduto != idProduto))
        {
            return null;
        }

        carrinho.DefinirQuantidade(idProduto, novaQuantidade);
        await pedidos.SalvarAsync(carrinho, cancellationToken);
        return carrinho;
    }

    public async Task<Pedido?> RemoverItemAsync(
        Guid tenantId, Guid? idCliente, Guid? idUnidade,
        Guid idProduto, CancellationToken cancellationToken = default)
    {
        var carrinho = await pedidos.ObterCarrinhoAbertoAsync(tenantId, idCliente, idUnidade, cancellationToken);
        if (carrinho is null || carrinho.Itens.All(i => i.IdProduto != idProduto))
        {
            return null;
        }

        carrinho.RemoverItem(idProduto);
        await pedidos.SalvarAsync(carrinho, cancellationToken);
        return carrinho;
    }
}
