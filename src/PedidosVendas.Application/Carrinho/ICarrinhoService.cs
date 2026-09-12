using PedidosVendas.Domain.Entities;

namespace PedidosVendas.Application.Carrinho;

/// <summary>
/// Ciclo de vida do carrinho (`Pedido` com `Status = Carrinho`). Todo método opera no
/// escopo (TenantId, IdCliente) do contexto — nunca do payload. Retorna nulo quando não
/// há carrinho/item naquele escopo (API traduz em 404).
/// </summary>
public interface ICarrinhoService
{
    Task<Pedido> ObterOuCriarAsync(
        Guid tenantId, Guid? idCliente, Guid? idUnidade, CancellationToken cancellationToken = default);

    Task<Pedido> AdicionarItemAsync(
        Guid tenantId, Guid? idCliente, Guid? idUnidade,
        Guid idProduto, int quantidade, CancellationToken cancellationToken = default);

    Task<Pedido?> DefinirQuantidadeAsync(
        Guid tenantId, Guid? idCliente, Guid? idUnidade,
        Guid idProduto, int novaQuantidade, CancellationToken cancellationToken = default);

    Task<Pedido?> RemoverItemAsync(
        Guid tenantId, Guid? idCliente, Guid? idUnidade,
        Guid idProduto, CancellationToken cancellationToken = default);
}
