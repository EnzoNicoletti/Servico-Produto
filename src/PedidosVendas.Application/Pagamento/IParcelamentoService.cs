namespace PedidosVendas.Application.Pagamento;

/// <summary>
/// Validação de parcelas (regra de backend, nunca só frontend): formas com
/// `QtdMaximaParcelas = 1` (PIX, transferência, depósito, débito) forçam 1 parcela;
/// parceláveis validam 1..limite (override do Tenant, senão o do catálogo).
/// </summary>
public interface IParcelamentoService
{
    Task<IReadOnlyList<FormaPagamentoItem>> ListarAsync(
        Guid tenantId, CancellationToken cancellationToken = default);

    Task<ParcelamentoValidado> ValidarAsync(
        Guid tenantId, Guid idFormaPagto, int quantidadeParcelas,
        CancellationToken cancellationToken = default);
}
