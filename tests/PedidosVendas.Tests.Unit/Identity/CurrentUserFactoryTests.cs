using System.Security.Claims;
using PedidosVendas.Application.Common.Identity;

namespace PedidosVendas.Tests.Unit;

public sealed class CurrentUserFactoryTests
{
    private static ClaimsPrincipal BuildPrincipal(
        bool authenticated = true,
        string? sub = "11111111-1111-1111-1111-111111111111",
        string? tenantId = "22222222-2222-2222-2222-222222222222",
        string? branchId = null,
        string[]? roles = null)
    {
        var claims = new List<Claim>();

        if (sub is not null)
        {
            claims.Add(new Claim(IdentityClaims.Subject, sub));
        }

        if (tenantId is not null)
        {
            claims.Add(new Claim(IdentityClaims.TenantId, tenantId));
        }

        if (branchId is not null)
        {
            claims.Add(new Claim(IdentityClaims.BranchId, branchId));
        }

        foreach (var role in roles ?? [])
        {
            claims.Add(new Claim(IdentityClaims.Role, role));
        }

        var identity = authenticated
            ? new ClaimsIdentity(claims, authenticationType: "TestAuth")
            : new ClaimsIdentity(claims);

        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public void Extrai_todas_as_informacoes_quando_claims_completas()
    {
        var principal = BuildPrincipal(
            sub: "0a000000-0000-0000-0000-000000000001",
            tenantId: "0a000000-0000-0000-0000-000000000002",
            branchId: "0a000000-0000-0000-0000-000000000003",
            roles: ["PedidosWrite", "PedidosRead"]);

        var user = CurrentUserFactory.FromClaimsPrincipal(principal);

        Assert.Equal(Guid.Parse("0a000000-0000-0000-0000-000000000001"), user.UserId);
        Assert.Equal(Guid.Parse("0a000000-0000-0000-0000-000000000002"), user.TenantId);
        Assert.Equal(Guid.Parse("0a000000-0000-0000-0000-000000000003"), user.BranchId);
        Assert.Equal(2, user.Roles.Count);
        Assert.Contains("PedidosWrite", user.Roles);
        Assert.Contains("PedidosRead", user.Roles);
        Assert.True(user.IsAuthenticated);
    }

    [Fact]
    public void Aceita_branch_ausente_como_nulo()
    {
        var principal = BuildPrincipal(branchId: null);

        var user = CurrentUserFactory.FromClaimsPrincipal(principal);

        Assert.Null(user.BranchId);
    }

    [Fact]
    public void Coleta_roles_de_ambos_os_tipos_de_claim_sem_duplicar()
    {
        var principal = BuildPrincipal(roles: ["PedidosRead"]);
        var identity = (ClaimsIdentity)principal.Identity!;
        identity.AddClaim(new Claim(IdentityClaims.RolesPlural, "pedidosadmin"));
        identity.AddClaim(new Claim(IdentityClaims.RolesPlural, "PedidosRead"));

        var user = CurrentUserFactory.FromClaimsPrincipal(principal);

        Assert.Equal(2, user.Roles.Count);
        Assert.Contains("PedidosRead", user.Roles);
        Assert.Contains("pedidosadmin", user.Roles);
    }

    [Fact]
    public void Rejeita_principal_nao_autenticado()
    {
        var principal = BuildPrincipal(authenticated: false);

        Assert.Throws<InvalidIdentityClaimsException>(
            () => CurrentUserFactory.FromClaimsPrincipal(principal));
    }

    [Fact]
    public void Rejeita_sub_ausente()
    {
        var principal = BuildPrincipal(sub: null);

        var exception = Assert.Throws<InvalidIdentityClaimsException>(
            () => CurrentUserFactory.FromClaimsPrincipal(principal));
        Assert.Contains("sub", exception.Message);
    }

    [Fact]
    public void Rejeita_tenant_id_ausente()
    {
        var principal = BuildPrincipal(tenantId: null);

        var exception = Assert.Throws<InvalidIdentityClaimsException>(
            () => CurrentUserFactory.FromClaimsPrincipal(principal));
        Assert.Contains("tenant_id", exception.Message);
    }

    [Fact]
    public void Rejeita_tenant_id_malformado()
    {
        var principal = BuildPrincipal(tenantId: "tenant-vindo-de-query-string");

        Assert.Throws<InvalidIdentityClaimsException>(
            () => CurrentUserFactory.FromClaimsPrincipal(principal));
    }

    [Fact]
    public void Rejeita_branch_id_malformado_quando_presente()
    {
        var principal = BuildPrincipal(branchId: "nao-e-guid");

        Assert.Throws<InvalidIdentityClaimsException>(
            () => CurrentUserFactory.FromClaimsPrincipal(principal));
    }
}
