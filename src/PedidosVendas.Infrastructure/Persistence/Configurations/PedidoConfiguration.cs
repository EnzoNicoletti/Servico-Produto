using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedidosVendas.Domain.Entities;

namespace PedidosVendas.Infrastructure.Persistence.Configurations;

public sealed class PedidoConfiguration : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> builder)
    {
        builder.ToTable("Pedido");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.TenantId).IsRequired();
        builder.Property(p => p.DataAbertura).IsRequired();
        builder.Property(p => p.Status).IsRequired();
        builder.Property(p => p.ValorTotal).HasPrecision(18, 2);

        // Concorrência otimista: coluna xmin do PostgreSQL (padrão da Etapa 02).
        builder.Property(p => p.RowVersion).IsRowVersion();

        // Localiza o carrinho aberto do cliente rapidamente.
        builder.HasIndex(p => new { p.TenantId, p.IdCliente, p.Status });

        builder.HasMany(p => p.Itens)
            .WithOne(i => i.Pedido)
            .HasForeignKey(i => i.IdPedido)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Itens).AutoInclude(false);
    }
}
