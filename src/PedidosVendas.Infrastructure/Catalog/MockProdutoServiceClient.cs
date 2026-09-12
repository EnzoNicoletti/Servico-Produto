using PedidosVendas.Application.Catalog;

namespace PedidosVendas.Infrastructure.Catalog;

/// <summary>
/// Implementação mock de <see cref="IProdutoServiceClient"/> para a Etapa 01.
/// Como o Servico-Estoque ainda não expõe endpoints de Produto/Categoria,
/// este mock permite desenvolver/testar o esqueleto sem acoplamento.
/// Na Etapa 03 será substituído por um client HTTP real, sem mudar o contrato.
/// </summary>
public sealed class MockProdutoServiceClient : IProdutoServiceClient
{
    private static readonly ProdutoDto ProdutoExemplo = new(
        Id: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        Nome: "Produto de exemplo (mock)",
        Preco: 99.90m,
        Disponivel: true);

    public Task<ProdutoDto?> ObterPorIdAsync(Guid produtoId, CancellationToken cancellationToken = default)
    {
        var resultado = produtoId == ProdutoExemplo.Id ? ProdutoExemplo : null;
        return Task.FromResult(resultado);
    }
}
