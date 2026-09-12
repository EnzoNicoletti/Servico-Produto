using PedidosVendas.Domain.Entities;

namespace PedidosVendas.Domain.Interfaces;

/// <summary>Catálogo global de formas de pagamento + overrides por Tenant.</summary>
public interface IFormaPagtoRepository
{
    Task<IReadOnlyList<FormaPagto>> ListarTodasAsync(CancellationToken cancellationToken = default);

    Task<FormaPagto?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TenantFormaPagtoConfig?> ObterOverrideAsync(
        Guid tenantId, Guid idFormaPagto, CancellationToken cancellationToken = default);
}
