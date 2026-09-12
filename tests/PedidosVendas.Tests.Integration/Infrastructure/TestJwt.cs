using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace PedidosVendas.Tests.Integration.Infrastructure;

/// <summary>
/// Emissor de tokens de teste assinados com a mesma chave simétrica configurada na
/// <see cref="PedidosVendasApiFactory"/> — simula o microsserviço de Identity.
/// </summary>
public static class TestJwt
{
    public const string SigningKey = "integration-tests-signing-key-0123456789abcdef-0123456789abcdef";
    public const string Issuer = "pedidosvendas-identity-tests";
    public const string Audience = "pedidosvendas-api";

    /// <summary>Chave diferente da oficial — usada para provar rejeição de emissor desconhecido.</summary>
    public const string ForeignSigningKey = "foreign-issuer-signing-key-fedcba9876543210-fedcba9876543210";
    public const string ForeignIssuer = "identity-desconhecido";

    public static string CreateToken(
        Guid userId,
        Guid? tenantId = null,
        Guid? branchId = null,
        IReadOnlyList<string>? roles = null,
        int validForMinutes = 5) =>
        CreateTokenInternal(
            signingKey: SigningKey,
            issuer: Issuer,
            audience: Audience,
            userId: userId,
            tenantId: tenantId,
            branchId: branchId,
            roles: roles,
            validForMinutes: validForMinutes);

    public static string CreateForeignIssuerToken(Guid userId, Guid tenantId) =>
        CreateTokenInternal(
            signingKey: ForeignSigningKey,
            issuer: ForeignIssuer,
            audience: Audience,
            userId: userId,
            tenantId: tenantId,
            branchId: null,
            roles: null,
            validForMinutes: 5);

    /// <summary>
    /// Token assinado corretamente, mas com <c>tenant_id</c> não-GUID —
    /// usado para provar que o middleware rejeita tenants malformados.
    /// </summary>
    public static string CreateTokenWithInvalidTenant(Guid userId) =>
        CreateTokenInternal(
            signingKey: SigningKey,
            issuer: Issuer,
            audience: Audience,
            userId: userId,
            tenantId: null,
            branchId: null,
            roles: null,
            validForMinutes: 5,
            extraClaims: new Dictionary<string, object>
            {
                [Application.Common.Identity.IdentityClaims.TenantId] = "tenant-vindo-de-query-string"
            });

    private static string CreateTokenInternal(
        string signingKey,
        string issuer,
        string audience,
        Guid userId,
        Guid? tenantId,
        Guid? branchId,
        IReadOnlyList<string>? roles,
        int validForMinutes,
        Dictionary<string, object>? extraClaims = null)
    {
        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = userId.ToString(),
            [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString()
        };

        if (tenantId.HasValue)
        {
            claims[Application.Common.Identity.IdentityClaims.TenantId] = tenantId.Value.ToString();
        }

        if (branchId.HasValue)
        {
            claims[Application.Common.Identity.IdentityClaims.BranchId] = branchId.Value.ToString();
        }

        if (roles is { Count: > 0 })
        {
            claims[Application.Common.Identity.IdentityClaims.Role] = roles.ToArray();
        }

        if (extraClaims is not null)
        {
            foreach (var (claimType, claimValue) in extraClaims)
            {
                claims[claimType] = claimValue;
            }
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            IssuedAt = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddMinutes(validForMinutes),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                SecurityAlgorithms.HmacSha256),
            Claims = claims
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    public static HttpRequestMessage ToAuthorizedRequest(string path, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return request;
    }
}
