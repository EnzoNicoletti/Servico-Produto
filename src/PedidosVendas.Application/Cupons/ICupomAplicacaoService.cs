using PedidosVendas.Domain.Entities;

namespace PedidosVendas.Application.Cupons;

/// <summary>
/// Aplicação de cupom ao carrinho (Etapa 04). Grava só `Pedido.IdCupom`; a quantidade do
/// cupom NÃO é decrementada aqui (só na finalização, Etapa 07). O desconto é sempre
/// recalculado — o vínculo é "grudento" (nunca removido automaticamente; se o carrinho
/// mudar e o cupom deixar de valer, o resumo passa a `Aplicavel = false` com motivo).
/// </summary>
public interface ICupomAplicacaoService
{
    /// <summary>
    /// Aplica o cupom ao carrinho aberto. Retorna nulo sem carrinho (404);
    /// lança <see cref="CupomNaoEncontradoException"/> (404) ou
    /// <see cref="CupomInaplicavelException"/> (400, com motivo).
    /// </summary>
    Task<(Pedido Pedido, ResumoDesconto Desconto)?> AplicarCupomAsync(
        Guid tenantId, Guid? idCliente, Guid? idUnidade,
        Guid idCupom, DateTime agoraUtc, CancellationToken cancellationToken = default);

    /// <summary>Remove o cupom do carrinho. Retorna nulo sem carrinho (404).</summary>
    Task<Pedido?> RemoverCupomAsync(
        Guid tenantId, Guid? idCliente, Guid? idUnidade,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calcula o desconto atual (para exibir e para a finalização). Retorna nulo sem
    /// carrinho (404); sem cupom vinculado retorna resumo zerado com motivo.
    /// </summary>
    Task<ResumoDesconto?> CalcularDescontoAsync(
        Guid tenantId, Guid? idCliente, Guid? idUnidade,
        DateTime agoraUtc, CancellationToken cancellationToken = default);
}
