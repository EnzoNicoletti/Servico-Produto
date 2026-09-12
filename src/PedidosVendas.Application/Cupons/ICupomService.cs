using PedidosVendas.Domain.Entities;

namespace PedidosVendas.Application.Cupons;

/// <summary>
/// CRUD de Cupom sempre no escopo do TenantId do contexto (nunca do payload).
/// Retorna nulo quando o cupom não existe naquele tenant (API traduz em 404,
/// sem vazar existência cross-tenant).
/// </summary>
public interface ICupomService
{
    Task<Cupom> CriarAsync(Guid tenantId, CupomDados dados, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Cupom> Itens, int Total)> ListarAsync(
        Guid tenantId, int pagina, int tamanhoPagina, CancellationToken cancellationToken = default);

    Task<Cupom?> ObterPorIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default);

    Task<Cupom?> AtualizarAsync(
        Guid id, Guid tenantId, CupomDados dados, CancellationToken cancellationToken = default);

    Task<bool> RemoverAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default);
}
