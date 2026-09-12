using PedidosVendas.Domain.Entities;

namespace PedidosVendas.Domain.Interfaces;

/// <summary>
/// Persistência do agregado Pedido. O carrinho é localizado por
/// (TenantId, IdCliente, Status, IdUnidade) — índice dedicado na migration.
/// </summary>
public interface IPedidoRepository
{
    Task<Pedido?> ObterCarrinhoAbertoAsync(
        Guid tenantId, Guid? idCliente, Guid? idUnidade, CancellationToken cancellationToken = default);

    Task<Pedido?> ObterPorIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default);

    Task AdicionarAsync(Pedido pedido, CancellationToken cancellationToken = default);

    Task SalvarAsync(Pedido pedido, CancellationToken cancellationToken = default);
}
