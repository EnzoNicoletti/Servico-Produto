using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedidosVendas.Domain.Entities;

namespace PedidosVendas.Infrastructure.Persistence.Configurations;

public sealed class TenantFormaPagtoConfigConfiguration : IEntityTypeConfiguration<TenantFormaPagtoConfig>
{
    public void Configure(EntityTypeBuilder<TenantFormaPagtoConfig> builder)
    {
        builder.ToTable("TenantFormaPagtoConfig", t =>
        {
            t.HasCheckConstraint("CK_TenantFormaPagtoConfig_QtdMinima", "\"QtdMaximaParcelasOverride\" >= 1");
        });

        builder.HasKey(c => c.Id);

        builder.Property(c => c.TenantId).IsRequired();
        builder.Property(c => c.IdFormaPagto).IsRequired();

        builder.HasIndex(c => new { c.TenantId, c.IdFormaPagto }).IsUnique();
        builder.HasIndex(c => c.TenantId);

        builder.HasOne(c => c.FormaPagto)
            .WithMany()
            .HasForeignKey(c => c.IdFormaPagto)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
