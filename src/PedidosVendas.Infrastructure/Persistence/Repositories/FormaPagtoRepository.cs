using Microsoft.EntityFrameworkCore;
using PedidosVendas.Domain.Entities;
using PedidosVendas.Domain.Interfaces;

namespace PedidosVendas.Infrastructure.Persistence.Repositories;

public sealed class FormaPagtoRepository(PedidosVendasDbContext context) : IFormaPagtoRepository
{
    public async Task<IReadOnlyList<FormaPagto>> ListarTodasAsync(CancellationToken cancellationToken = default)
        => await context.FormasPagto
            .AsNoTracking()
            .OrderBy(f => f.Descricao)
            .ToListAsync(cancellationToken);

    public Task<FormaPagto?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        => context.FormasPagto
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public Task<TenantFormaPagtoConfig?> ObterOverrideAsync(
        Guid tenantId, Guid idFormaPagto, CancellationToken cancellationToken = default)
        => context.TenantFormaPagtoConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.TenantId == tenantId && c.IdFormaPagto == idFormaPagto, cancellationToken);
}
