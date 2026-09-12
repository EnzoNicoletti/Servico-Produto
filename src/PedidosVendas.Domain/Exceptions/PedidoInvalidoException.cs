namespace PedidosVendas.Domain.Exceptions;

/// <summary>
/// Lançada quando uma invariante do agregado Pedido é violada
/// (quantidade, item ausente, transições). Na API vira 400.
/// </summary>
public sealed class PedidoInvalidoException : Exception
{
    public PedidoInvalidoException(string message)
        : base(message)
    {
    }
}
