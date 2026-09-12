using PedidosVendas.Application.Catalog;

namespace PedidosVendas.Infrastructure.Catalog;

/// <summary>
/// Implementação mock de <see cref="IProdutoServiceClient"/> para as Etapas 01-03.
/// Como o Servico-Estoque ainda não expõe endpoints de Produto/Categoria,
/// este mock permite desenvolver/testar sem acoplamento: um produto disponível,
/// um indisponível e nulo para qualquer outro id.
/// A implementação HTTP real entra quando o Estoque expuser o contrato, sem mudar a interface.
/// </summary>
public sealed class MockProdutoServiceClient : IProdutoServiceClient
{
    public static readonly Guid ProdutoDisponivelId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public static readonly Guid ProdutoIndisponivelId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public static readonly Guid CategoriaDoProdutoDisponivelId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private static readonly ProdutoDto ProdutoExemplo = new(
        Id: ProdutoDisponivelId,
        Nome: "Produto de exemplo (mock)",
        Preco: 99.90m,
        Disponivel: true,
        IdCategoria: CategoriaDoProdutoDisponivelId);

    private static readonly ProdutoDto ProdutoEsgotado = new(
        Id: ProdutoIndisponivelId,
        Nome: "Produto esgotado (mock)",
        Preco: 49.90m,
        Disponivel: false);

    public Task<ProdutoDto?> ObterPorIdAsync(Guid produtoId, CancellationToken cancellationToken = default)
    {
        ProdutoDto? resultado = produtoId switch
        {
            _ when produtoId == ProdutoExemplo.Id => ProdutoExemplo,
            _ when produtoId == ProdutoEsgotado.Id => ProdutoEsgotado,
            _ => null
        };

        return Task.FromResult(resultado);
    }
}
