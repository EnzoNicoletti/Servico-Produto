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

    /// <summary>
    /// Consome uma unidade do cupom com concorrência otimista (para a finalização).
    /// Implementado e testado na Etapa 04, mas chamado em produção só na Etapa 07.
    /// Lança <see cref="CupomNaoEncontradoException"/> (ausente) ou
    /// <see cref="Domain.Exceptions.CupomInvalidoException"/> (esgotado ou conflito —
    /// erro de negócio claro, nunca exceção de infra).
    /// </summary>
    Task ConsumirAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default);
}
