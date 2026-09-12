using Microsoft.EntityFrameworkCore;
using PedidosVendas.Domain.Entities;

namespace PedidosVendas.Infrastructure.Persistence;

/// <summary>
/// DbContext do microsserviço PedidosVendas.
/// Etapa 02: agregado Cupom (+CupomProduto). Pedido/Venda entram nas Etapas 03-07.
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PedidosVendasDbContext).Assembly);
    }
}
