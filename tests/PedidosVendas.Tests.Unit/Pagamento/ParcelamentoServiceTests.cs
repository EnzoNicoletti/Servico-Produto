using PedidosVendas.Application.Pagamento;
using PedidosVendas.Domain.Entities;
using PedidosVendas.Domain.Interfaces;

namespace PedidosVendas.Tests.Unit.Pagamento;

public sealed class ParcelamentoServiceTests
{
    private static readonly Guid TenantA = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TenantB = Guid.Parse("88888888-8888-8888-8888-888888888888");

    private sealed class RepositorioFalso : IFormaPagtoRepository
    {
        public readonly FormaPagto Pix = new("PIX", 1);
        public readonly FormaPagto Transferencia = new("Transferência", 1);
        public readonly FormaPagto Deposito = new("Depósito", 1);
        public readonly FormaPagto Debito = new("Cartão de Débito", 1);
        public readonly FormaPagto Credito = new("Cartão de Crédito", 12);
        public readonly Dictionary<(Guid Tenant, Guid Forma), int> Overrides = [];

        public Task<IReadOnlyList<FormaPagto>> ListarTodasAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<FormaPagto>>([Pix, Transferencia, Deposito, Debito, Credito]);

        public Task<FormaPagto?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(ListarTodasAsync(ct).Result.FirstOrDefault(f => f.Id == id));

        public Task<TenantFormaPagtoConfig?> ObterOverrideAsync(Guid tenantId, Guid idFormaPagto, CancellationToken ct = default) =>
            Task.FromResult(Overrides.TryGetValue((tenantId, idFormaPagto), out var qtd)
                ? new TenantFormaPagtoConfig(tenantId, idFormaPagto, qtd)
                : null);
    }

    private readonly RepositorioFalso _repo = new();
    private ParcelamentoService Sut => new(_repo);

    [Theory]
    [InlineData(2)]
    [InlineData(12)]
    public async Task AVista_com_outra_quantidade_e_rejeitado(int quantidade)
    {
        foreach (var forma in new[] { _repo.Pix, _repo.Transferencia, _repo.Deposito, _repo.Debito })
        {
            var exception = await Assert.ThrowsAsync<ParcelamentoInvalidoException>(
                () => Sut.ValidarAsync(TenantA, forma.Id, quantidade));
            Assert.Contains("1 parcela", exception.Message);
        }
    }

    [Fact]
    public async Task AVista_com_1_parcela_ok()
    {
        var resultado = await Sut.ValidarAsync(TenantA, _repo.Pix.Id, 1);

        Assert.Equal(1, resultado.QuantidadeParcelas);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(12)]
    public async Task Credito_no_limite_padrao_ok(int quantidade)
    {
        var resultado = await Sut.ValidarAsync(TenantA, _repo.Credito.Id, quantidade);

        Assert.Equal(quantidade, resultado.QuantidadeParcelas);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public async Task Credito_fora_do_limite_rejeitado(int quantidade)
    {
        var exception = await Assert.ThrowsAsync<ParcelamentoInvalidoException>(
            () => Sut.ValidarAsync(TenantA, _repo.Credito.Id, quantidade));

        Assert.Contains("1 a 12", exception.Message);
    }

    [Fact]
    public async Task Override_do_tenant_prevalece_sobre_padrao()
    {
        _repo.Overrides[(TenantA, _repo.Credito.Id)] = 6;

        var ok = await Sut.ValidarAsync(TenantA, _repo.Credito.Id, 6);
        Assert.Equal(6, ok.QuantidadeParcelas);

        var exception = await Assert.ThrowsAsync<ParcelamentoInvalidoException>(
            () => Sut.ValidarAsync(TenantA, _repo.Credito.Id, 7));
        Assert.Contains("1 a 6", exception.Message);

        // Outro tenant segue o padrão.
        var outro = await Sut.ValidarAsync(TenantB, _repo.Credito.Id, 12);
        Assert.Equal(12, outro.QuantidadeParcelas);
    }

    [Fact]
    public async Task Forma_desconhecida_lanca_nao_encontrada()
    {
        await Assert.ThrowsAsync<FormaPagtoNaoEncontradaException>(
            () => Sut.ValidarAsync(TenantA, Guid.NewGuid(), 1));
    }

    [Fact]
    public async Task Listar_resolve_limite_por_tenant()
    {
        _repo.Overrides[(TenantA, _repo.Credito.Id)] = 6;

        var listaA = await Sut.ListarAsync(TenantA);
        var listaB = await Sut.ListarAsync(TenantB);

        Assert.Equal(5, listaA.Count);
        Assert.Equal(6, listaA.First(i => i.Descricao == "Cartão de Crédito").QtdMaximaParcelas);
        Assert.Equal(12, listaB.First(i => i.Descricao == "Cartão de Crédito").QtdMaximaParcelas);
        Assert.All(listaA, i => Assert.True(i.QtdMaximaParcelas >= 1));
    }
}
