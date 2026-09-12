using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedidosVendas.Domain.Entities;

namespace PedidosVendas.Infrastructure.Persistence.Configurations;

public sealed class CupomConfiguration : IEntityTypeConfiguration<Cupom>
{
    public void Configure(EntityTypeBuilder<Cupom> builder)
    {
        builder.ToTable("Cupom", t =>
        {
            // Apenas um entre ValorDesconto/PercDesconto pode ser > 0 (espelha a regra de domínio).
            t.HasCheckConstraint(
                "CK_Cupom_DescontoExclusivo",
                "(\"ValorDesconto\" > 0 AND \"PercDesconto\" = 0) OR (\"PercDesconto\" > 0 AND \"ValorDesconto\" = 0)");
            t.HasCheckConstraint("CK_Cupom_QuantidadeNaoNegativa", "\"Quantidade\" >= 0");
        });

        builder.HasKey(c => c.Id);

        builder.Property(c => c.TenantId).IsRequired();
        builder.Property(c => c.Descricao).IsRequired().HasMaxLength(200);
        builder.Property(c => c.ValorDesconto).HasPrecision(18, 2);
        builder.Property(c => c.PercDesconto).HasPrecision(5, 2);
        builder.Property(c => c.ValorMinimoCompra).HasPrecision(18, 2);
        builder.Property(c => c.DataValidade).IsRequired();
        builder.Property(c => c.DataCriacao).IsRequired();

        // Concorrência otimista (roadmap 4.9): coluna xmin do PostgreSQL.
        builder.Property(c => c.RowVersion).IsRowVersion();

        builder.HasIndex(c => c.TenantId);
        builder.HasIndex(c => new { c.TenantId, c.DataValidade });

        builder.HasMany(c => c.CupomProdutos)
            .WithOne(cp => cp.Cupom)
            .HasForeignKey(cp => cp.IdCupom)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(c => c.CupomProdutos).AutoInclude(false);
    }
}
