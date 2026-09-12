using Microsoft.EntityFrameworkCore;
using PedidosVendas.Domain.Entities;

namespace PedidosVendas.Infrastructure.Persistence;

/// <summary>
/// DbContext do microsserviço PedidosVendas.
/// Etapa 02: agregado Cupom. Etapa 03: agregado Pedido. Venda entra nas Etapas 06-07.
/// Configurado para PostgreSQL via Npgsql (ver DependencyInjection).
/// </summary>
public sealed class PedidosVendasDbContext : DbContext
{
    public PedidosVendasDbContext(DbContextOptions<PedidosVendasDbContext> options)
        : base(options)
    {
    }

    public DbSet<Cupom> Cupons => Set<Cupom>();

    public DbSet<CupomProduto> CupomProdutos => Set<CupomProduto>();

    public DbSet<Pedido> Pedidos => Set<Pedido>();

    public DbSet<ProdutosPedido> ProdutosPedido => Set<ProdutosPedido>();

    public DbSet<FormaPagto> FormasPagto => Set<FormaPagto>();

    public DbSet<TenantFormaPagtoConfig> TenantFormaPagtoConfigs => Set<TenantFormaPagtoConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PedidosVendasDbContext).Assembly);
    }
}
