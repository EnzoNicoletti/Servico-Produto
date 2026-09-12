namespace PedidosVendas.Application.Catalog;

/// <summary>
/// Porta de saída para consulta de produtos/categorias no microsserviço de Estoque.
/// O Servico-Estoque ainda não expõe nenhum endpoint de Produto/Categoria
/// (só GET /api/v1/me de diagnóstico) — por isso a Etapa 01 registra aqui apenas
/// o contrato + implementação mock. A implementação HTTP real entra na Etapa 03,
/// reaproveitando este mesmo contrato (regra fundamental: não inventar padrão novo).
/// </summary>
public interface IProdutoServiceClient
{
    /// <summary>Busca um produto por id. Retorna nulo quando não encontrado.</summary>
    Task<ProdutoDto?> ObterPorIdAsync(Guid produtoId, CancellationToken cancellationToken = default);
}
