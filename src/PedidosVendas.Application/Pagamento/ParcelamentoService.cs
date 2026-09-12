using PedidosVendas.Domain.Interfaces;

namespace PedidosVendas.Application.Pagamento;

public sealed class ParcelamentoService(IFormaPagtoRepository formas) : IParcelamentoService
{
    public async Task<IReadOnlyList<FormaPagamentoItem>> ListarAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        var todas = await formas.ListarTodasAsync(cancellationToken);
        var itens = new List<FormaPagamentoItem>(todas.Count);

        foreach (var forma in todas)
        {
            itens.Add(new FormaPagamentoItem(
                forma.Id, forma.Descricao, await LimiteAsync(tenantId, forma.Id, forma.QtdMaximaParcelas, cancellationToken)));
        }

        return itens;
    }

    public async Task<ParcelamentoValidado> ValidarAsync(
        Guid tenantId, Guid idFormaPagto, int quantidadeParcelas,
        CancellationToken cancellationToken = default)
    {
        var forma = await formas.ObterPorIdAsync(idFormaPagto, cancellationToken)
            ?? throw new FormaPagtoNaoEncontradaException(idFormaPagto);

        // À vista (PIX, transferência, depósito, débito): 1 parcela, sem exceção —
        // qualquer outro valor vindo do cliente é rejeitado aqui no backend.
        if (!forma.PermiteParcelar)
        {
            if (quantidadeParcelas != 1)
            {
                throw new ParcelamentoInvalidoException(
                    $"A forma '{forma.Descricao}' aceita apenas 1 parcela (à vista).");
            }

            return new ParcelamentoValidado(forma.Id, forma.Descricao, 1);
        }

        var limite = await LimiteAsync(tenantId, forma.Id, forma.QtdMaximaParcelas, cancellationToken);
        if (quantidadeParcelas < 1 || quantidadeParcelas > limite)
        {
            throw new ParcelamentoInvalidoException(
                $"A forma '{forma.Descricao}' permite de 1 a {limite} parcelas para este Tenant.");
        }

        return new ParcelamentoValidado(forma.Id, forma.Descricao, quantidadeParcelas);
    }

    private async Task<int> LimiteAsync(
        Guid tenantId, Guid idFormaPagto, int padrao, CancellationToken cancellationToken)
    {
        var config = await formas.ObterOverrideAsync(tenantId, idFormaPagto, cancellationToken);
        return config?.QtdMaximaParcelasOverride ?? padrao;
    }
}
