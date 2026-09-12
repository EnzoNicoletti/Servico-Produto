using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PedidosVendas.Application.Common.Identity;
using System.Text;

namespace PedidosVendas.API.Authentication;

/// <summary>
/// Aplica as <see cref="JwtOptions"/> (vinculadas à configuração de forma lazy, já com
/// overrides de ambiente/testes) sobre o <see cref="JwtBearerOptions"/>.
/// </summary>
internal sealed class ConfigureJwtBearerOptions(IOptions<JwtOptions> jwtOptions)
    : IConfigureNamedOptions<JwtBearerOptions>
{
    public void Configure(string? name, JwtBearerOptions options)
    {
        if (!string.Equals(name, JwtBearerDefaults.AuthenticationScheme, StringComparison.Ordinal))
        {
            return;
        }

        Configure(options);
    }

    public void Configure(JwtBearerOptions options)
    {
        var jwt = jwtOptions.Value;

        options.RequireHttpsMetadata = jwt.RequireHttpsMetadata;

        // Mantém os nomes originais das claims (sub, tenant_id, branch_id, role),
        // evitando remapeamentos legados do WIF (ex.: sub -> nameidentifier).
        options.MapInboundClaims = false;

        if (!string.IsNullOrWhiteSpace(jwt.SigningKey))
        {
            options.TokenValidationParameters = BuildSymmetricParameters(jwt);
        }
        else if (!string.IsNullOrWhiteSpace(jwt.Authority))
        {
            options.Authority = jwt.Authority;
            options.TokenValidationParameters = BuildAuthorityParameters(jwt);
        }
    }

    private static TokenValidationParameters CreateCommonParameters(JwtOptions jwt) => new()
    {
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(jwt.ClockSkewSeconds),

        NameClaimType = IdentityClaims.Subject,
        RoleClaimType = IdentityClaims.Role,

        // Algoritmos aceitos para assinatura dos tokens do Identity.
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256, SecurityAlgorithms.RsaSha256]
    };

    /// <summary>Modo desenvolvimento/testes: chave simétrica HS256 compartilhada com o emissor.</summary>
    private static TokenValidationParameters BuildSymmetricParameters(JwtOptions jwt)
    {
        var parameters = CreateCommonParameters(jwt);
        parameters.ValidateIssuer = true;
        parameters.ValidIssuer = jwt.ValidIssuer;
        parameters.ValidateAudience = true;
        parameters.ValidAudience = jwt.Audience;
        parameters.IssuerSigningKey =
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey));

        return parameters;
    }

    /// <summary>Modo produção: validação da assinatura e do emissor via documento OIDC/JWKS do Identity.</summary>
    private static TokenValidationParameters BuildAuthorityParameters(JwtOptions jwt)
    {
        var parameters = CreateCommonParameters(jwt);
        parameters.ValidateAudience = true;
        parameters.ValidAudience = jwt.Audience;

        // Sem issuer explícito, o emissor é validado contra o documento OIDC do Authority.
        parameters.ValidateIssuer = !string.IsNullOrWhiteSpace(jwt.ValidIssuer);
        parameters.ValidIssuer = jwt.ValidIssuer;

        return parameters;
    }
}
