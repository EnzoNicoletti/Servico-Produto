namespace PedidosVendas.Application.Frete;

/// <summary>Cotação de frete para um CEP de destino.</summary>
public sealed record CotacaoFrete(
    string CepDestino,
    decimal Valor,
    int PrazoDiasUteis);
