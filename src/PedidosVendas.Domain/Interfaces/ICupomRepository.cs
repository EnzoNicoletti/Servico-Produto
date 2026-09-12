using PedidosVendas.Domain.Entities;

namespace PedidosVendas.Domain.Interfaces;

/// <summary>
/// Contrato de persistência do agregado Cupom (mesmo papel do IBranchRepository
/// no Identity: filtro sempre por TenantId, AsNoTracking nas leituras).
/// </summary>
public interface ICupomRepository
{
    Task<Cupom?> ObterPorIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default);

    Task<Cupom?> ObterParaAtualizacaoAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Cupom> Itens, int Total)> ListarAsync(
        Guid tenantId, int pagina, int tamanhoPagina, CancellationToken cancellationToken = default);

    Task AdicionarAsync(Cupom cupom, CancellationToken cancellationToken = default);

    Task AtualizarAsync(Cupom cupom, CancellationToken cancellationToken = default);

    Task RemoverAsync(Cupom cupom, CancellationToken cancellationToken = default);
}
