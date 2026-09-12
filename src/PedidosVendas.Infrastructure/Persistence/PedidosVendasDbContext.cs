using Microsoft.EntityFrameworkCore;

namespace PedidosVendas.Infrastructure.Persistence;

/// <summary>
/// DbContext do microsserviço PedidosVendas (Etapa 01 — esqueleto).
/// Intencionalmente SEM entidades: Cupom/Pedido/Venda entram nas Etapas 02-07.
/// Configurado para PostgreSQL via Npgsql (ver DependencyInjection).
/// </summary>
public sealed class PedidosVendasDbContext : DbContext
{
    public PedidosVendasDbContext(DbContextOptions<PedidosVendasDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Etapa 01: nenhum DbSet. As entidades de negócio (Cupom, CupomProduto,
        // Pedido, ProdutosPedido, Venda, ProdutosVenda, FormaPagto) serão
        // adicionadas nas próximas etapas com suas configurações/migrations.
    }
}
