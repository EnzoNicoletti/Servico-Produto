using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PedidosVendas.API.Middleware;
using PedidosVendas.Application.Common.Identity;
using PedidosVendas.Application.Common.Interfaces;

namespace PedidosVendas.API.Services;

/// <summary>
/// Fonte de verdade do usuário/tenant da requisição corrente.
/// Lê o <see cref="CurrentUser"/> resolvido pelo <see cref="TenantResolutionMiddleware"/>
/// e cacheado em <c>HttpContext.Items</c>; fora de uma requisição autenticada retorna nulo
/// (propriedades das interfaces lançam exceção explicativa nesse caso).
/// </summary>
public sealed class CurrentUserProvider(IHttpContextAccessor httpContextAccessor)
    : ICurrentUserService,
      ITenantContext
{
    private CurrentUser? _currentUser;
    private bool _resolved;
    private ClaimsPrincipal? _observedPrincipal;

    /// <summary>Usuário corrente ou nulo quando não há requisição autenticada.</summary>
    public CurrentUser? ResolveOrNull()
    {
        var httpContext = httpContextAccessor.HttpContext;

        // Re-resolve apenas se o principal mudou dentro do mesmo escopo (caso raro em testes).
        if (_resolved && ReferenceEquals(_observedPrincipal, httpContext?.User))
        {
            return _currentUser;
        }

        var principal = httpContext?.User;

        if (principal?.Identity?.IsAuthenticated != true)
        {
            _currentUser = null;
        }
        else if (httpContext!.Items.TryGetValue(TenantResolutionMiddleware.CurrentUserItemKey, out var cached) &&
                 cached is CurrentUser cachedUser)
        {
            _currentUser = cachedUser;
        }
        else
        {
            // Fallback: pipeline executado sem o middleware (ex.: uso direto em testes unitários).
            _currentUser = CurrentUserFactory.FromClaimsPrincipal(principal);
        }

        _resolved = true;
        _observedPrincipal = principal;
        return _currentUser;
    }

    /// <summary>Usuário corrente garantido; lança se acessado fora de uma requisição autenticada.</summary>
    public CurrentUser RequireCurrentUser() =>
        ResolveOrNull()
        ?? throw new InvalidOperationException(
            "Não há um usuário autenticado na requisição corrente. "
            + "ICurrentUserService/ITenantContext só podem ser consumidos no contexto de um request autenticado.");

    public bool IsAuthenticated => ResolveOrNull()?.IsAuthenticated ?? false;

    public Guid UserId => RequireCurrentUser().UserId;

    public Guid TenantId => RequireCurrentUser().TenantId;

    public Guid? BranchId => RequireCurrentUser().BranchId;

    public IReadOnlyList<string> Roles => RequireCurrentUser().Roles;
}
