using Microsoft.EntityFrameworkCore;
using PedidosVendas.Domain.Entities;
using PedidosVendas.Domain.Exceptions;
using PedidosVendas.Domain.Interfaces;

namespace PedidosVendas.Infrastructure.Persistence.Repositories;

/// <summary>
/// Persistência do agregado Cupom (mesmo estilo do BranchRepository no Identity:
/// filtro sempre por TenantId, AsNoTracking nas leituras, SaveChanges no repositório).
/// </summary>
public sealed class CupomRepository(PedidosVendasDbContext context) : ICupomRepository
{
    public Task<Cupom?> ObterPorIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
        => context.Cupons
            .AsNoTracking()
            .Include(c => c.CupomProdutos)
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId, cancellationToken);

    public Task<Cupom?> ObterParaAtualizacaoAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
        => context.Cupons
            .Include(c => c.CupomProdutos)
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId, cancellationToken);

    public async Task<(IReadOnlyList<Cupom> Itens, int Total)> ListarAsync(
        Guid tenantId, int pagina, int tamanhoPagina, CancellationToken cancellationToken = default)
    {
        var query = context.Cupons
            .AsNoTracking()
            .Include(c => c.CupomProdutos)
            .Where(c => c.TenantId == tenantId)
            .OrderByDescending(c => c.DataCriacao);

        var total = await query.CountAsync(cancellationToken);
        var itens = await query
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(cancellationToken);

        return (itens, total);
    }

    public async Task AdicionarAsync(Cupom cupom, CancellationToken cancellationToken = default)
    {
        context.Cupons.Add(cupom);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Persiste um cupom previamente carregado por <see cref="ObterParaAtualizacaoAsync"/>.
    /// Os vínculos são reconciliados explicitamente: links novos recebem Add() e links
    /// removidos recebem Remove(). Sem isso, um link novo (Guid já preenchido no ctor)
    /// descoberto na coleção é lido pelo EF como existente e gera um UPDATE fantasma
    /// (0 linhas afetadas → DbUpdateConcurrencyException). Ver MEMORIA_ETAPA_02.
    /// </summary>
    public async Task AtualizarAsync(Cupom cupom, CancellationToken cancellationToken = default)
    {
        var atuais = await context.CupomProdutos
            .Where(cp => cp.IdCupom == cupom.Id)
            .ToListAsync(cancellationToken);

        var desejados = cupom.CupomProdutos.Select(cp => cp.IdProduto).ToHashSet();

        foreach (var velho in atuais.Where(x => !desejados.Contains(x.IdProduto)))
        {
            context.CupomProdutos.Remove(velho);
        }

        var existentes = atuais.Select(x => x.IdProduto).ToHashSet();
        foreach (var novo in cupom.CupomProdutos.Where(x => !existentes.Contains(x.IdProduto)))
        {
            context.CupomProdutos.Add(novo);
        }

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Traduz o conflito otimista (xmin) em erro de domínio: a Application
            // converte em erro de negócio claro, sem vazar exceção de infra.
            throw new ConcorrenciaException(
                "O registro foi alterado por outra operação. Recarregue e tente novamente.");
        }
    }

    public async Task RemoverAsync(Cupom cupom, CancellationToken cancellationToken = default)
    {
        context.Cupons.Remove(cupom);
        await context.SaveChangesAsync(cancellationToken);
    }
}
