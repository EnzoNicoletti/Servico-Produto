namespace PedidosVendas.API.Authentication;

/// <summary>
/// Opções de validação dos tokens emitidos pelo microsserviço de Identity.
/// Dois modos suportados (mutuamente exclusivos) — mesmo padrão do Servico-Estoque:
/// - <see cref="Authority"/>: validação via OIDC/JWKS do Identity (produção);
/// - <see cref="SigningKey"/>: chave simétrica HS256 compartilhada (desenvolvimento local/testes).
/// Este serviço apenas VALIDA tokens (nenhuma emissão/login aqui).
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Authentication:Jwt";

    /// <summary>Base URL do Identity emissor dos tokens (ex.: https://identity.sistema.local). Opcional no modo chave simétrica.</summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>Audiência esperada nos tokens destinados a este serviço.</summary>
    public string Audience { get; set; } = "pedidosvendas-api";

    /// <summary>Emissor esperado quando conhecido. No modo Authority sem valor, o emissor é validado pelo documento OIDC.</summary>
    public string ValidIssuer { get; set; } = string.Empty;

    /// <summary>Chave simétrica HS256 para ambientes sem o Identity no ar (dev/testes). Mínimo de 32 bytes.</summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Exigir HTTPS ao recuperar metadados do Authority.</summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>Tolerância de relógio na validação de expiração, em segundos.</summary>
    public int ClockSkewSeconds { get; set; } = 60;

    /// <summary>Indica se há um modo de validação configurado.</summary>
    public bool HasValidationMode =>
        !string.IsNullOrWhiteSpace(Authority) || !string.IsNullOrWhiteSpace(SigningKey);
}
