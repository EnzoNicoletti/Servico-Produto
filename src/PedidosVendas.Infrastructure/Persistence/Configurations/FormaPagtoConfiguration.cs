using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedidosVendas.Domain.Entities;

namespace PedidosVendas.Infrastructure.Persistence.Configurations;

public sealed class FormaPagtoConfiguration : IEntityTypeConfiguration<FormaPagto>
{
    public void Configure(EntityTypeBuilder<FormaPagto> builder)
    {
        builder.ToTable("FormaPagto", t =>
        {
            t.HasCheckConstraint("CK_FormaPagto_QtdMinima", "\"QtdMaximaParcelas\" >= 1");
        });

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Descricao).IsRequired().HasMaxLength(100);

        builder.HasIndex(f => f.Descricao).IsUnique();
    }
}
