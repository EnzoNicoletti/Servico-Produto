namespace PedidosVendas.Application.Pagamento;

/// <summary>Forma de pagamento inexistente (API traduz em 404).</summary>
public sealed class FormaPagtoNaoEncontradaException(Guid id)
    : Exception($"Forma de pagamento {id} não encontrada.");
