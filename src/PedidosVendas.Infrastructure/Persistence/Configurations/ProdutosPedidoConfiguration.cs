using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedidosVendas.Domain.Entities;

namespace PedidosVendas.Infrastructure.Persistence.Configurations;

public sealed class ProdutosPedidoConfiguration : IEntityTypeConfiguration<ProdutosPedido>
{
    public void Configure(EntityTypeBuilder<ProdutosPedido> builder)
    {
        builder.ToTable("ProdutosPedido", t =>
        {
            t.HasCheckConstraint("CK_ProdutosPedido_QuantidadePositiva", "\"Quantidade\" > 0");
        });

        builder.HasKey(i => i.Id);

        builder.Property(i => i.IdPedido).IsRequired();
        builder.Property(i => i.IdProduto).IsRequired();
        builder.Property(i => i.ValorUnitario).HasPrecision(18, 2);

        // Um produto aparece no máximo uma vez por pedido (adicionar soma quantidade).
        builder.HasIndex(i => new { i.IdPedido, i.IdProduto }).IsUnique();
        builder.HasIndex(i => i.IdProduto);
    }
}
