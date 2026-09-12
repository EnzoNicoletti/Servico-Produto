namespace PedidosVendas.Domain.Exceptions;

/// <summary>
/// Lançada quando uma invariante do domínio de Cupom é violada
/// (desconto exclusivo, coerência produto-vínculos, tenant, validade, quantidade).
/// Na API é traduzida em 400 (ValidationProblem).
/// </summary>
public sealed class CupomInvalidoException : Exception
{
    public CupomInvalidoException(string message)
        : base(message)
    {
    }
}
