namespace PedidosVendas.Domain.Exceptions;

/// <summary>Invariante de FormaPagto violada (API traduz em 400).</summary>
public sealed class FormaPagtoInvalidaException(string message) : Exception(message);
