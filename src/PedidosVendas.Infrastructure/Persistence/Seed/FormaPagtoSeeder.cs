using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PedidosVendas.Domain.Entities;
using PedidosVendas.Infrastructure.Persistence;

namespace PedidosVendas.Infrastructure.Persistence.Seed;

/// <summary>
/// Seed do catálogo de formas de pagamento (mesmo padrão do RoleSeeder no Identity:
/// idempotente por descrição, executado no bloco Development após o migrate).
/// Ids não são fixos: consumidores resolvem pela listagem, nunca hardcoded.
/// </summary>
public static class FormaPagtoSeeder
{
    public static readonly (string Descricao, int QtdMaximaParcelas)[] Catalogo =
    [
        ("PIX", 1),
        ("Transferência", 1),
        ("Depósito", 1),
        ("Cartão de Débito", 1),
        ("Cartão de Crédito", 12),
    ];

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PedidosVendasDbContext>();

        var existentes = await db.FormasPagto
            .Select(f => f.Descricao)
            .ToListAsync();

        foreach (var (descricao, qtd) in Catalogo.Where(c => !existentes.Contains(c.Descricao)))
        {
            db.FormasPagto.Add(new FormaPagto(descricao, qtd));
        }

        await db.SaveChangesAsync();
    }
}
