using PedidosVendas.Infrastructure.Catalog;

namespace PedidosVendas.Tests.Unit;

public sealed class MockProdutoServiceClientTests
{
    private readonly MockProdutoServiceClient _client = new();

    [Fact]
    public async Task Retorna_produto_mock_para_id_conhecido()
    {
        var produto = await _client.ObterPorIdAsync(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

        Assert.NotNull(produto);
        Assert.True(produto.Disponivel);
        Assert.True(produto.Preco > 0);
    }

    [Fact]
    public async Task Retorna_nulo_para_id_desconhecido()
    {
        var produto = await _client.ObterPorIdAsync(Guid.NewGuid());

        Assert.Null(produto);
    }

    [Fact]
    public async Task Retorna_produto_indisponivel_para_id_conhecido()
    {
        var produto = await _client.ObterPorIdAsync(MockProdutoServiceClient.ProdutoIndisponivelId);

        Assert.NotNull(produto);
        Assert.False(produto.Disponivel);
    }
}
