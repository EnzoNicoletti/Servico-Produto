using System.Text.Json;
using Microsoft.AspNetCore.Http;
using PedidosVendas.Application.Common.Identity;

namespace PedidosVendas.API.Middleware;

/// <summary>
/// Garante a resolução segura do contexto de tenant:
/// - qualquer requisição AUTENTICADA sem a claim <c>tenant_id</c> (ou com valor não-GUID,
///   ou com <c>sub</c>/<c>branch_id</c> inválidos) é rejeitada com 403;
/// - o resultado é cacheado em <see cref="HttpContext.Items"/> para consumo do
///   <see cref="Services.CurrentUserProvider"/> (fonte única: claims do JWT —
///   nunca query string, headers customizados ou body).
/// </summary>
public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    public const string CurrentUserItemKey = "PedidosVendas.CurrentUser";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            CurrentUser currentUser;

            try
            {
                currentUser = CurrentUserFactory.FromClaimsPrincipal(context.User);
            }
            catch (InvalidIdentityClaimsException exception)
            {
                await WriteForbiddenAsync(context, exception.Message);
                return;
            }

            context.Items[CurrentUserItemKey] = currentUser;
        }

        await next(context);
    }

    private static async Task WriteForbiddenAsync(HttpContext context, string detail)
    {
        var problem = new
        {
            type = "https://tools.ietf.org/html/rfc9110#section-6.5.3",
            title = "Contexto de tenant inválido",
            status = StatusCodes.Status403Forbidden,
            detail
        };

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }
}
