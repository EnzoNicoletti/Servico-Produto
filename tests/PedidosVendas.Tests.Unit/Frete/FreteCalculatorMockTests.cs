using Microsoft.Extensions.Options;
using PedidosVendas.Application.Frete;
using PedidosVendas.Infrastructure.Frete;

namespace PedidosVendas.Tests.Unit.Frete;

public sealed class FreteCalculatorMockTests
{
    private static FreteCalculatorMock NovoMock(decimal valorBase = 12, decimal valorPorUnidade = 2) =>
        new(Options.Create(new FreteOptions { ValorBase = valorBase, ValorPorUnidade = valorPorUnidade }));

    private static List<ItemFrete> Itens(params (int Quantidade, decimal Valor)[] itens) =>
        itens.Select((i, idx) => new ItemFrete(Guid.NewGuid(), i.Quantidade, i.Valor)).ToList();

    [Fact]
    public async Task Mesmo_parametro_retorna_mesmo_resultado()
    {
        var sut = NovoMock();
        var itens = Itens((2, 100), (1, 50));

        var primeira = await sut.CalcularAsync("01310-100", itens, null);
        var segunda = await sut.CalcularAsync("01310100", itens, null);

        Assert.Equal(primeira, segunda);
        Assert.Equal("01310100", primeira.CepDestino);
        Assert.Equal(18, primeira.Valor); // 12 + 2 × 3 unidades
    }

    [Theory]
    [InlineData("01310100", 2)]
    [InlineData("40000000", 4)]
    [InlineData("90000000", 6)]
    public async Task Prazo_por_faixa_de_CEP(string cep, int prazoEsperado)
    {
        var cotacao = await NovoMock().CalcularAsync(cep, Itens((1, 10)), null);

        Assert.Equal(prazoEsperado, cotacao.PrazoDiasUteis);
    }

    [Fact]
    public async Task Carrinho_vazio_cota_zero()
    {
        var cotacao = await NovoMock().CalcularAsync("01310100", [], null);

        Assert.Equal(0, cotacao.Valor);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("1234567")]
    [InlineData("123456789")]
    public async Task Cep_invalido_lanca_erro_claro(string cep)
    {
        var exception = await Assert.ThrowsAsync<CepInvalidoException>(
            () => NovoMock().CalcularAsync(cep, Itens((1, 10)), null));

        Assert.Contains("8 dígitos", exception.Message);
    }

    [Fact]
    public async Task Valores_vem_da_configuracao()
    {
        var cotacao = await NovoMock(valorBase: 5, valorPorUnidade: 1)
            .CalcularAsync("01310100", Itens((2, 100)), null);

        Assert.Equal(7, cotacao.Valor);
    }

    [Fact]
    public async Task Origem_nula_ou_informada_produz_mesma_cotacao()
    {
        var sut = NovoMock();
        var itens = Itens((2, 100));

        var semOrigem = await sut.CalcularAsync("01310100", itens, null);
        var comOrigem = await sut.CalcularAsync("01310100", itens, Guid.NewGuid());

        Assert.Equal(semOrigem, comOrigem);
    }

    [Fact]
    public async Task Itens_nulos_rejeitados()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => NovoMock().CalcularAsync("01310100", null!, null));
    }
}
