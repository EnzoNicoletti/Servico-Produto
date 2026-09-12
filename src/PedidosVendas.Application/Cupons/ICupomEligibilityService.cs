using PedidosVendas.Domain.Entities;

namespace PedidosVendas.Application.Cupons;

/// <summary>
/// Elegibilidade e aplicabilidade de cupom (lógica pura, sem Pedido — Etapa 04 integra).
/// Prioridade determinística (roadmap 4.4): produto específico &gt; categoria &gt; global.
/// </summary>
public interface ICupomEligibilityService
{
    /// <summary>Retorna os itens aos quais o desconto do cupom se aplica.</summary>
    IReadOnlyList<CarrinhoItem> ObterItensElegiveis(Cupom cupom, IEnumerable<CarrinhoItem> itens);

    /// <summary>
    /// Valida se o cupom pode ser aplicado: tenant, validade, quantidade e valor mínimo
    /// (subtotal recebido como parâmetro — ainda não há Pedido real).
    /// </summary>
    bool PodeAplicar(Cupom cupom, Guid tenantId, decimal subtotal, DateTime agoraUtc);

    /// <summary>Calcula o desconto sobre os itens elegíveis (fixo, limitado ao subtotal elegível; ou percentual).</summary>
    decimal CalcularDesconto(Cupom cupom, IEnumerable<CarrinhoItem> itensElegiveis);
}
