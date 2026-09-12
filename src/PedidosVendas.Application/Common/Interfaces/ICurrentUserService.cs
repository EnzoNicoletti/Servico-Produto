namespace PedidosVendas.Application.Common.Interfaces;

/// <summary>
/// Abstrai o usuário autenticado da requisição corrente.
/// Os valores são extraídos exclusivamente das claims do JWT validado —
/// nunca de query string, headers customizados ou body.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>Indica se há um usuário autenticado na requisição corrente.</summary>
    bool IsAuthenticated { get; }

    /// <summary>Identificador do usuário (claim <c>sub</c>).</summary>
    Guid UserId { get; }

    /// <summary>Papéis do usuário (claims <c>role</c>/<c>roles</c>).</summary>
    IReadOnlyList<string> Roles { get; }
}
