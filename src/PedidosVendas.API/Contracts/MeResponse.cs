namespace PedidosVendas.API.Contracts;

/// <summary>
/// Retorno do endpoint GET /api/v1/me — espelha as claims do token autenticado.
/// Serve de contrato de verificação da extração de claims (ICurrentUserService/ITenantContext).
/// </summary>
public sealed record MeResponse(
    Guid UserId,
    Guid TenantId,
    Guid? BranchId,
    IReadOnlyList<string> Roles);
