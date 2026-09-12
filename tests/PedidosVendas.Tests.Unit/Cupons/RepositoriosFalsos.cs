using PedidosVendas.Domain.Entities;
using PedidosVendas.Domain.Interfaces;

namespace PedidosVendas.Tests.Unit.Cupons;

/// <summary>Repositórios em memória para testar a aplicação sem banco.</summary>
internal sealed class PedidoRepositorioFalso : IPedidoRepository
{
    private readonly Dictionary<Guid, Pedido> _porId = [];

    public void AdicionarDireto(Pedido pedido) => _porId[pedido.Id] = pedido;

    public Task<Pedido?> ObterCarrinhoAbertoAsync(
        Guid tenantId, Guid? idCliente, Guid? idUnidade, CancellationToken cancellationToken = default) =>
        Task.FromResult(_porId.Values.FirstOrDefault(p =>
            p.TenantId == tenantId && p.IdCliente == idCliente && p.IdUnidade == idUnidade
            && p.Status == Domain.Enums.StatusPedido.Carrinho));

    public Task<Pedido?> ObterPorIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_porId.TryGetValue(id, out var pedido) && pedido.TenantId == tenantId ? pedido : null);

    public Task AdicionarAsync(Pedido pedido, CancellationToken cancellationToken = default)
    {
        _porId[pedido.Id] = pedido;
        return Task.CompletedTask;
    }

    public Task SalvarAsync(Pedido pedido, CancellationToken cancellationToken = default)
    {
        _porId[pedido.Id] = pedido;
        return Task.CompletedTask;
    }
}

internal sealed class CupomRepositorioFalso : ICupomRepository
{
    private readonly Dictionary<Guid, Cupom> _porId = [];

    public void AdicionarDireto(Cupom cupom) => _porId[cupom.Id] = cupom;

    public Task<Cupom?> ObterPorIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_porId.TryGetValue(id, out var cupom) && cupom.TenantId == tenantId ? cupom : null);

    public Task<Cupom?> ObterParaAtualizacaoAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default) =>
        ObterPorIdAsync(id, tenantId, cancellationToken);

    public Task<(IReadOnlyList<Cupom> Itens, int Total)> ListarAsync(
        Guid tenantId, int pagina, int tamanhoPagina, CancellationToken cancellationToken = default)
    {
        var itens = _porId.Values.Where(c => c.TenantId == tenantId).ToList();
        return Task.FromResult<(IReadOnlyList<Cupom>, int)>((itens, itens.Count));
    }

    public Task AdicionarAsync(Cupom cupom, CancellationToken cancellationToken = default)
    {
        _porId[cupom.Id] = cupom;
        return Task.CompletedTask;
    }

    public Task AtualizarAsync(Cupom cupom, CancellationToken cancellationToken = default)
    {
        _porId[cupom.Id] = cupom;
        return Task.CompletedTask;
    }

    public Task RemoverAsync(Cupom cupom, CancellationToken cancellationToken = default)
    {
        _porId.Remove(cupom.Id);
        return Task.CompletedTask;
    }
}
