using PedidosVendas.Application.Common.Interfaces;

namespace PedidosVendas.Application.Common.Identity;

/// <summary>
/// Representação imutável do usuário/tenant da requisição corrente,
/// extraído exclusivamente das claims do JWT validado.
/// </summary>
/// <param name="UserId">Claim <c>sub</c>.</param>
/// <param name="TenantId">Claim <c>tenant_id</c>.</param>
/// <param name="BranchId">Claim <c>branch_id</c>, quando presente.</param>
/// <param name="Roles">Claims de papel (<c>role</c>/<c>roles</c>).</param>
/// <param name="IsAuthenticated">Sempre <c>true</c> quando construído via factory.</param>
public sealed record CurrentUser(
    Guid UserId,
    Guid TenantId,
    Guid? BranchId,
    IReadOnlyList<string> Roles,
    bool IsAuthenticated = true) : ICurrentUserService, ITenantContext;
