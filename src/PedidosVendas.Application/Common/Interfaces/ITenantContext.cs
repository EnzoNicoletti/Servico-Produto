namespace PedidosVendas.Application.Common.Interfaces;

/// <summary>
/// Contexto de multi-tenancy da requisição corrente, resolvido a partir
/// das claims <c>tenant_id</c> e <c>branch_id</c> do JWT validado.
/// O <see cref="TenantId"/> nunca pode ser aceito de qualquer outra fonte.
/// </summary>
public interface ITenantContext
{
    /// <summary>Identificador do tenant (claim <c>tenant_id</c>). Garantido pelo middleware.</summary>
    Guid TenantId { get; }

    /// <summary>Identificador da filial/unidade (claim <c>branch_id</c>), quando presente no token.</summary>
    Guid? BranchId { get; }
}
