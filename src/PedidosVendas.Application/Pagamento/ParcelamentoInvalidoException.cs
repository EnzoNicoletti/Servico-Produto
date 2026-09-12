namespace PedidosVendas.Application.Pagamento;

/// <summary>Parcelamento inválido para a forma de pagamento (API traduz em 400).</summary>
public sealed class ParcelamentoInvalidoException(string motivo) : Exception(motivo);
