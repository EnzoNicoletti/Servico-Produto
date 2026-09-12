using PedidosVendas.Domain.Entities;
using PedidosVendas.Domain.Exceptions;

namespace PedidosVendas.Tests.Unit.Cupons;

public sealed class CupomTests
{
    private static readonly Guid Tenant = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTime Hoje = new(2026, 9, 12, 0, 0, 0, DateTimeKind.Utc);

    private static Cupom CriarGlobal(
        decimal valorDesconto = 10,
        decimal percDesconto = 0,
        int quantidade = 5,
        DateTime? validade = null) =>
        Cupom.Criar(
            Tenant, "10 OFF", valorDesconto, percDesconto, 0, null,
            validade ?? Hoje.AddDays(30), quantidade, false, [],
            agoraUtc: Hoje);

    [Fact]
    public void Criar_global_valido_preenche_campos()
    {
        var cupom = CriarGlobal();

        Assert.NotEqual(Guid.Empty, cupom.Id);
        Assert.Equal(Tenant, cupom.TenantId);
        Assert.Equal("10 OFF", cupom.Descricao);
        Assert.Equal(10, cupom.ValorDesconto);
        Assert.Equal(0, cupom.PercDesconto);
        Assert.Equal(Hoje, cupom.DataCriacao);
        Assert.Empty(cupom.CupomProdutos);
    }

    [Fact]
    public void Criar_por_produto_vincula_produtos()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();

        var cupom = Cupom.Criar(
            Tenant, "Produto", 0, 15, 0, null, Hoje.AddDays(10), 3, true, [p1, p2],
            agoraUtc: Hoje);

        Assert.True(cupom.IsCupomProduto);
        Assert.Equal([p1, p2], cupom.CupomProdutos.Select(cp => cp.IdProduto).Order().ToList());
        Assert.All(cupom.CupomProdutos, cp => Assert.Equal(cupom.Id, cp.IdCupom));
    }

    [Theory]
    [InlineData(10, 5)] // ambos preenchidos
    [InlineData(0, 0)] // nenhum preenchido
    public void Criar_rejeita_desconto_nao_exclusivo(decimal valor, decimal percentual)
    {
        var exception = Assert.Throws<CupomInvalidoException>(
            () => Cupom.Criar(Tenant, "X", valor, percentual, 0, null, Hoje.AddDays(1), 1, false, [], agoraUtc: Hoje));

        Assert.Contains("exatamente um", exception.Message);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -5)]
    public void Criar_rejeita_desconto_negativo(decimal valor, decimal percentual)
    {
        Assert.Throws<CupomInvalidoException>(
            () => Cupom.Criar(Tenant, "X", valor, percentual, 0, null, Hoje.AddDays(1), 1, false, [], agoraUtc: Hoje));
    }

    [Fact]
    public void Criar_rejeita_tenant_vazio_e_descricao_vazia()
    {
        Assert.Throws<CupomInvalidoException>(
            () => Cupom.Criar(Guid.Empty, "X", 10, 0, 0, null, Hoje.AddDays(1), 1, false, [], agoraUtc: Hoje));

        Assert.Throws<CupomInvalidoException>(
            () => Cupom.Criar(Tenant, "  ", 10, 0, 0, null, Hoje.AddDays(1), 1, false, [], agoraUtc: Hoje));
    }

    [Fact]
    public void Criar_rejeita_validade_no_passado_e_quantidade_negativa()
    {
        Assert.Throws<CupomInvalidoException>(
            () => Cupom.Criar(Tenant, "X", 10, 0, 0, null, Hoje.AddDays(-1), 1, false, [], agoraUtc: Hoje));

        Assert.Throws<CupomInvalidoException>(
            () => Cupom.Criar(Tenant, "X", 10, 0, 0, null, Hoje.AddDays(1), -1, false, [], agoraUtc: Hoje));
    }

    [Fact]
    public void Criar_exige_coerencia_entre_flag_e_vinculos()
    {
        // Flag por produto sem vínculos.
        Assert.Throws<CupomInvalidoException>(
            () => Cupom.Criar(Tenant, "X", 10, 0, 0, null, Hoje.AddDays(1), 1, true, [], agoraUtc: Hoje));

        // Vínculos sem flag.
        Assert.Throws<CupomInvalidoException>(
            () => Cupom.Criar(Tenant, "X", 10, 0, 0, null, Hoje.AddDays(1), 1, false, [Guid.NewGuid()], agoraUtc: Hoje));
    }

    [Fact]
    public void RegistrarUso_decrementa_e_lanca_quando_esgotado()
    {
        var cupom = CriarGlobal(quantidade: 1);

        cupom.RegistrarUso();
        Assert.Equal(0, cupom.Quantidade);

        Assert.Throws<CupomInvalidoException>(() => cupom.RegistrarUso());
    }

    [Fact]
    public void Atualizar_troca_campos_e_revalida()
    {
        var cupom = CriarGlobal();
        var categoria = Guid.NewGuid();

        cupom.Atualizar("Novo", 0, 20, 100, categoria, Hoje.AddDays(60), 7, false, [], agoraUtc: Hoje);

        Assert.Equal("Novo", cupom.Descricao);
        Assert.Equal(0, cupom.ValorDesconto);
        Assert.Equal(20, cupom.PercDesconto);
        Assert.Equal(100, cupom.ValorMinimoCompra);
        Assert.Equal(categoria, cupom.IdCategoria);

        // Revalida: dois descontos na atualização também é rejeitado.
        Assert.Throws<CupomInvalidoException>(
            () => cupom.Atualizar("X", 5, 5, 0, null, Hoje.AddDays(1), 1, false, [], agoraUtc: Hoje));
    }
}
