namespace PedidosVendas.Application.Common.Identity;

/// <summary>
/// Lançada quando o principal autenticado não contém as claims obrigatórias
/// (<c>sub</c>, <c>tenant_id</c>) ou quando elas não representam GUIDs válidos.
/// Traduzida pelo middleware de tenant em resposta HTTP 403.
/// </summary>
public sealed class InvalidIdentityClaimsException : Exception
{
    public InvalidIdentityClaimsException(string message)
        : base(message)
    {
    }
}
