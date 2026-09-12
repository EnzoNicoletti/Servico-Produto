using PedidosVendas.Application.Frete;

namespace PedidosVendas.API.Contracts.Carrinho;

/// <summary>
/// Cotação preliminar (Etapa 05): valor exibido, NÃO persistido — recalculado na
/// finalização (Etapa 07) para evitar divergência entre cotação e cobrança.
/// </summary>
public sealed record FreteResponse(
    string CepDestino,
    decimal Valor,
    int PrazoDiasUteis)
{
    public static FreteResponse From(CotacaoFrete cotacao) => new(
        cotacao.CepDestino, cotacao.Valor, cotacao.PrazoDiasUteis);
}
