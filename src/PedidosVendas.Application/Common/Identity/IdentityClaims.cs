namespace PedidosVendas.Application.Common.Identity;

/// <summary>
/// Nomes canônicos das claims emitidas pelo microsserviço de Identity
/// e consumidas por este serviço (mesmo padrão do Servico-Estoque).
/// </summary>
public static class IdentityClaims
{
    /// <summary>Identificador do usuário.</summary>
    public const string Subject = "sub";

    /// <summary>Identificador do tenant. Fonte única de verdade — nunca query string, header ou body.</summary>
    public const string TenantId = "tenant_id";

    /// <summary>Identificador da filial/unidade (opcional).</summary>
    public const string BranchId = "branch_id";

    /// <summary>Claim padrão de papel.</summary>
    public const string Role = "role";

    /// <summary>Variação alternativa de papel aceita para compatibilidade com emissores que pluralizam a claim.</summary>
    public const string RolesPlural = "roles";
}
