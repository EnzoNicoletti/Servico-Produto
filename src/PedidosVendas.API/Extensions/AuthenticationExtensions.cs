using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using PedidosVendas.API.Authentication;

namespace PedidosVendas.API.Extensions;

/// <summary>
/// Autenticação JWT: este serviço apenas VALIDA tokens emitidos pelo
/// microsserviço de Identity já existente (nenhuma emissão/login aqui).
/// O binding das opções é LAZY (BindConfiguration): a configuração final da aplicação —
/// incluindo overrides de variáveis de ambiente e de testes — já está disponível quando
/// as opções são resolvidas. Regras estruturais são validadas no startup (ValidateOnStart).
/// </summary>
public static class AuthenticationExtensions
{
    public static IServiceCollection AddApiAuthentication(this IServiceCollection services)
    {
        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .Validate(
                jwt => jwt.HasValidationMode,
                "Configure Authentication:Jwt:Authority (produção) ou Authentication:Jwt:SigningKey + Authentication:Jwt:ValidIssuer (desenvolvimento/testes).")
            .Validate(
                jwt => string.IsNullOrWhiteSpace(jwt.SigningKey)
                    || (Encoding.UTF8.GetByteCount(jwt.SigningKey) >= 32
                        && !string.IsNullOrWhiteSpace(jwt.ValidIssuer)),
                "Ao usar Authentication:Jwt:SigningKey, informe ValidIssuer e uma chave de pelo menos 32 caracteres (256 bits para HS256).")
            .ValidateOnStart();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddTransient<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();

        return services;
    }
}
