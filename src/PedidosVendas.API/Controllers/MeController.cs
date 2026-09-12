using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PedidosVendas.API.Contracts;
using PedidosVendas.Application.Common.Interfaces;

namespace PedidosVendas.API.Controllers;

/// <summary>
/// Endpoint mínimo de verificação da fundação (Etapa 01):
/// exige autenticação + tenant válido e devolve as claims resolvidas.
/// Também serve ao teste de 401 (sem JWT) exigido pela etapa.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/me")]
public sealed class MeController(ICurrentUserService currentUser, ITenantContext tenantContext) : ControllerBase
{
    [HttpGet]
    public ActionResult<MeResponse> Get() =>
        Ok(new MeResponse(
            UserId: currentUser.UserId,
            TenantId: tenantContext.TenantId,
            BranchId: tenantContext.BranchId,
            Roles: currentUser.Roles));
}
