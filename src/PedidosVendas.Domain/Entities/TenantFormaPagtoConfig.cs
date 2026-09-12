using PedidosVendas.Domain.Exceptions;

namespace PedidosVendas.Domain.Entities;

/// <summary>
/// Override do limite de parcelas de uma forma de pagamento para um Tenant
/// (fallback local — decisão #3: o Identity não possui esse campo).
/// Usado apenas quando existe linha; na ausência vale `FormaPagto.QtdMaximaParcelas`.
/// </summary>
public sealed class TenantFormaPagtoConfig
{
    private TenantFormaPagtoConfig()
    {
        // EF Core.
    }

    public TenantFormaPagtoConfig(Guid tenantId, Guid idFormaPagto, int qtdMaximaParcelasOverride)
    {
        if (tenantId == Guid.Empty)
        {
            throw new FormaPagtoInvalidaException("TenantId é obrigatório.");
        }

        if (idFormaPagto == Guid.Empty)
        {
            throw new FormaPagtoInvalidaException("IdFormaPagto é obrigatório.");
        }

        if (qtdMaximaParcelasOverride < 1)
        {
            throw new FormaPagtoInvalidaException("QtdMaximaParcelasOverride deve ser ao menos 1.");
        }

        Id = Guid.NewGuid();
        TenantId = tenantId;
        IdFormaPagto = idFormaPagto;
        QtdMaximaParcelasOverride = qtdMaximaParcelasOverride;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid IdFormaPagto { get; private set; }

    public int QtdMaximaParcelasOverride { get; private set; }

    public FormaPagto FormaPagto { get; private set; } = null!;
}
