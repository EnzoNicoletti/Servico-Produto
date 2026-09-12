using System.Security.Claims;
using PedidosVendas.Application.Common.Interfaces;

namespace PedidosVendas.Application.Common.Identity;

/// <summary>
/// Constrói o <see cref="CurrentUser"/> a partir das claims de um principal já autenticado.
/// Regras (idênticas às do Servico-Estoque):
/// - <c>sub</c> e <c>tenant_id</c> são obrigatórios e devem ser GUIDs válidos;
/// - <c>branch_id</c> é opcional; se presente, também deve ser GUID válido;
/// - papéis são coletados das claims <c>role</c> e/ou <c>roles</c>, sem duplicidade.
/// Qualquer violação lança <see cref="InvalidIdentityClaimsException"/>.
/// </summary>
public static class CurrentUserFactory
{
    public static CurrentUser FromClaimsPrincipal(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        if (principal.Identity?.IsAuthenticated != true)
        {
            throw new InvalidIdentityClaimsException("O principal informado não está autenticado.");
        }

        var userId = GetRequiredGuid(principal, IdentityClaims.Subject);
        var tenantId = GetRequiredGuid(principal, IdentityClaims.TenantId);
        var branchId = TryGetGuid(principal, IdentityClaims.BranchId);

        var roles = principal.FindAll(IdentityClaims.Role)
            .Concat(principal.FindAll(IdentityClaims.RolesPlural))
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new CurrentUser(
            UserId: userId,
            TenantId: tenantId,
            BranchId: branchId,
            Roles: roles.AsReadOnly(),
            IsAuthenticated: true);
    }

    private static Guid GetRequiredGuid(ClaimsPrincipal principal, string claimType)
    {
        var value = principal.FindFirst(claimType)?.Value;

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidIdentityClaimsException($"A claim obrigatória '{claimType}' não foi encontrada no token.");
        }

        if (!Guid.TryParse(value, out var parsed))
        {
            throw new InvalidIdentityClaimsException($"A claim '{claimType}' não contém um GUID válido.");
        }

        return parsed;
    }

    private static Guid? TryGetGuid(ClaimsPrincipal principal, string claimType)
    {
        var value = principal.FindFirst(claimType)?.Value;

        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!Guid.TryParse(value, out var parsed))
        {
            throw new InvalidIdentityClaimsException($"A claim '{claimType}' está presente, mas não contém um GUID válido.");
        }

        return parsed;
    }
}
