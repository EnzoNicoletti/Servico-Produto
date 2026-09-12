using PedidosVendas.Application.Catalog;
using PedidosVendas.Domain.Entities;
using PedidosVendas.Domain.Interfaces;

namespace PedidosVendas.Application.Cupons;

public sealed class CupomAplicacaoService(
    IPedidoRepository pedidos,
    ICupomRepository cupons,
    ICupomEligibilityService eligibility,
    IProdutoServiceClient produtos) : ICupomAplicacaoService
{
    public async Task<(Pedido Pedido, ResumoDesconto Desconto)?> AplicarCupomAsync(
        Guid tenantId, Guid? idCliente, Guid? idUnidade,
        Guid idCupom, DateTime agoraUtc, CancellationToken cancellationToken = default)
    {
        var carrinho = await pedidos.ObterCarrinhoAbertoAsync(tenantId, idCliente, idUnidade, cancellationToken);
        if (carrinho is null)
        {
            return null;
        }

        var cupom = await cupons.ObterPorIdAsync(idCupom, tenantId, cancellationToken)
            ?? throw new CupomNaoEncontradoException(idCupom);

        var motivo = MotivoInaplicavel(cupom, tenantId, carrinho.ValorTotal, agoraUtc);
        if (motivo is not null)
        {
            throw new CupomInaplicavelException(motivo);
        }

        carrinho.AplicarCupom(cupom.Id);
        await pedidos.SalvarAsync(carrinho, cancellationToken);

        return (carrinho, await MontarResumoAsync(cupom, carrinho, agoraUtc, cancellationToken));
    }

    public async Task<Pedido?> RemoverCupomAsync(
        Guid tenantId, Guid? idCliente, Guid? idUnidade,
        CancellationToken cancellationToken = default)
    {
        var carrinho = await pedidos.ObterCarrinhoAbertoAsync(tenantId, idCliente, idUnidade, cancellationToken);
        if (carrinho is null)
        {
            return null;
        }

        carrinho.RemoverCupom();
        await pedidos.SalvarAsync(carrinho, cancellationToken);
        return carrinho;
    }

    public async Task<ResumoDesconto?> CalcularDescontoAsync(
        Guid tenantId, Guid? idCliente, Guid? idUnidade,
        DateTime agoraUtc, CancellationToken cancellationToken = default)
    {
        var carrinho = await pedidos.ObterCarrinhoAbertoAsync(tenantId, idCliente, idUnidade, cancellationToken);
        if (carrinho is null)
        {
            return null;
        }

        if (carrinho.IdCupom is null)
        {
            return ResumoDesconto.SemCupom("Nenhum cupom aplicado ao carrinho.");
        }

        var cupom = await cupons.ObterPorIdAsync(carrinho.IdCupom.Value, tenantId, cancellationToken);
        if (cupom is null)
        {
            return ResumoDesconto.SemCupom("Cupom vinculado não encontrado.");
        }

        return await MontarResumoAsync(cupom, carrinho, agoraUtc, cancellationToken);
    }

    private async Task<ResumoDesconto> MontarResumoAsync(
        Cupom cupom, Pedido carrinho, DateTime agoraUtc, CancellationToken cancellationToken)
    {
        var motivo = MotivoInaplicavel(cupom, carrinho.TenantId, carrinho.ValorTotal, agoraUtc);
        var itens = await MapearItensAsync(carrinho, cancellationToken);
        var elegiveis = eligibility.ObterItensElegiveis(cupom, itens);

        if (motivo is not null || elegiveis.Count == 0)
        {
            return new ResumoDesconto(
                false,
                motivo ?? "Nenhum item do carrinho é elegível ao cupom.",
                0,
                elegiveis.Sum(i => i.Quantidade * i.ValorUnitario),
                []);
        }

        var total = eligibility.CalcularDesconto(cupom, elegiveis);
        var subtotal = elegiveis.Sum(i => i.Quantidade * i.ValorUnitario);

        // Rateio proporcional do desconto entre os itens elegíveis (sem arredondar:
        // a soma das parcelas equivale ao total; arredondamento é da Etapa 07).
        var parcelas = elegiveis.Select(i => new ItemDesconto(
            i.IdProduto,
            i.Quantidade,
            i.ValorUnitario,
            subtotal == 0 ? 0 : total * ((i.Quantidade * i.ValorUnitario) / subtotal))).ToList();

        return new ResumoDesconto(true, null, total, subtotal, parcelas);
    }

    private static string? MotivoInaplicavel(Cupom cupom, Guid tenantId, decimal subtotal, DateTime agoraUtc)
    {
        if (cupom.TenantId != tenantId)
        {
            return "Cupom de outro tenant.";
        }

        if (agoraUtc.Date > cupom.DataValidade.Date)
        {
            return "Cupom expirado.";
        }

        if (cupom.Quantidade <= 0)
        {
            return "Cupom esgotado.";
        }

        if (subtotal < cupom.ValorMinimoCompra)
        {
            return $"Valor mínimo de compra não atingido (mínimo {cupom.ValorMinimoCompra:C}).";
        }

        return null;
    }

    /// <summary>
    /// Mapeia `ProdutosPedido → CarrinhoItem`: preço sempre do snapshot (decisão 4.8),
    /// categoria resolvida viva via Estoque (Etapa 04); produto fora do catálogo mantém o
    /// snapshot e perde a categoria (só cupons globais/por produto o alcançam).
    /// </summary>
    private async Task<List<CarrinhoItem>> MapearItensAsync(Pedido carrinho, CancellationToken cancellationToken)
    {
        var itens = new List<CarrinhoItem>(carrinho.Itens.Count);
        foreach (var item in carrinho.Itens)
        {
            var produto = await produtos.ObterPorIdAsync(item.IdProduto, cancellationToken);
            itens.Add(new CarrinhoItem(item.IdProduto, produto?.IdCategoria, item.Quantidade, item.ValorUnitario));
        }

        return itens;
    }
}
