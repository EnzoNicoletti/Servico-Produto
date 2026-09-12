using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedidosVendas.Domain.Entities;

namespace PedidosVendas.Infrastructure.Persistence.Configurations;

public sealed class CupomProdutoConfiguration : IEntityTypeConfiguration<CupomProduto>
{
    public void Configure(EntityTypeBuilder<CupomProduto> builder)
    {
        builder.ToTable("CupomProduto");

        builder.HasKey(cp => cp.Id);

        builder.Property(cp => cp.IdCupom).IsRequired();
        builder.Property(cp => cp.IdProduto).IsRequired();

        // Um produto não se vincula duas vezes ao mesmo cupom.
        builder.HasIndex(cp => new { cp.IdCupom, cp.IdProduto }).IsUnique();
        builder.HasIndex(cp => cp.IdProduto);
    }
}
