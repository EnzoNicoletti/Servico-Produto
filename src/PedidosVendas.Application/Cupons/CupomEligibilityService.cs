using PedidosVendas.Domain.Entities;

namespace PedidosVendas.Application.Cupons;

public sealed class CupomEligibilityService : ICupomEligibilityService
{
    public IReadOnlyList<CarrinhoItem> ObterItensElegiveis(Cupom cupom, IEnumerable<CarrinhoItem> itens)
    {
        ArgumentNullException.ThrowIfNull(cupom);
        ArgumentNullException.ThrowIfNull(itens);

        var lista = itens.ToList();

        // 1. Produto específico (via CupomProduto) tem prioridade sobre todo o resto.
        var vinculados = cupom.CupomProdutos.Select(cp => cp.IdProduto).ToHashSet();
        if (vinculados.Count > 0)
        {
            return lista.Where(i => vinculados.Contains(i.IdProduto)).ToList();
        }

        // 2. Categoria.
        if (cupom.IdCategoria.HasValue)
        {
            return lista.Where(i => i.IdCategoria == cupom.IdCategoria).ToList();
        }

        // 3. Global: todos os itens.
        return lista;
    }

    public bool PodeAplicar(Cupom cupom, Guid tenantId, decimal subtotal, DateTime agoraUtc)
    {
        ArgumentNullException.ThrowIfNull(cupom);

        if (cupom.TenantId != tenantId)
        {
            return false;
        }

        if (agoraUtc.Date > cupom.DataValidade.Date)
        {
            return false;
        }

        if (cupom.Quantidade <= 0)
        {
            return false;
        }

        if (subtotal < cupom.ValorMinimoCompra)
        {
            return false;
        }

        return true;
    }

    public decimal CalcularDesconto(Cupom cupom, IEnumerable<CarrinhoItem> itensElegiveis)
    {
        ArgumentNullException.ThrowIfNull(cupom);
        ArgumentNullException.ThrowIfNull(itensElegiveis);

        var subtotalElegivel = itensElegiveis.Sum(i => i.Quantidade * i.ValorUnitario);
        if (subtotalElegivel <= 0)
        {
            return 0;
        }

        // Valor fixo: limitado ao subtotal elegível (desconto nunca gera valor negativo).
        if (cupom.ValorDesconto > 0)
        {
            return Math.Min(cupom.ValorDesconto, subtotalElegivel);
        }

        return subtotalElegivel * (cupom.PercDesconto / 100);
    }
}
