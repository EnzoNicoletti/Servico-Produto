using Microsoft.EntityFrameworkCore;
using PedidosVendas.Domain.Entities;
using PedidosVendas.Domain.Enums;
using PedidosVendas.Domain.Interfaces;

namespace PedidosVendas.Infrastructure.Persistence.Repositories;

/// <summary>
/// Persistência do agregado Pedido. Leituras com AsNoTracking; a gravação de mutações
/// usa reconciliação explícita dos itens (mesma lição da Etapa 02: links novos com Guid
/// preenchido descobertos na coleção viram UPDATE fantasma — aqui Add/Remove explícitos).
/// </summary>
public sealed class PedidoRepository(PedidosVendasDbContext context) : IPedidoRepository
{
    public Task<Pedido?> ObterCarrinhoAbertoAsync(
        Guid tenantId, Guid? idCliente, Guid? idUnidade, CancellationToken cancellationToken = default)
        => context.Pedidos
            .Include(p => p.Itens)
            .FirstOrDefaultAsync(
                p => p.TenantId == tenantId
                    && p.IdCliente == idCliente
                    && p.IdUnidade == idUnidade
                    && p.Status == StatusPedido.Carrinho,
                cancellationToken);

    public Task<Pedido?> ObterPorIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
        => context.Pedidos
            .Include(p => p.Itens)
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId, cancellationToken);

    public async Task AdicionarAsync(Pedido pedido, CancellationToken cancellationToken = default)
    {
        context.Pedidos.Add(pedido);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task SalvarAsync(Pedido pedido, CancellationToken cancellationToken = default)
    {
        var atuais = await context.ProdutosPedido
            .Where(i => i.IdPedido == pedido.Id)
            .ToListAsync(cancellationToken);

        var desejados = pedido.Itens.Select(i => i.IdProduto).ToHashSet();

        foreach (var velho in atuais.Where(x => !desejados.Contains(x.IdProduto)))
        {
            context.ProdutosPedido.Remove(velho);
        }

        var existentes = atuais.Select(x => x.IdProduto).ToHashSet();
        foreach (var novo in pedido.Itens.Where(x => !existentes.Contains(x.IdProduto)))
        {
            context.ProdutosPedido.Add(novo);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
