using PedidosVendas.Domain.Entities;
using PedidosVendas.Domain.Interfaces;

namespace PedidosVendas.Application.Cupons;

public sealed class CupomService(ICupomRepository repository) : ICupomService
{
    public async Task<Cupom> CriarAsync(Guid tenantId, CupomDados dados, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dados);

        var cupom = Cupom.Criar(
            tenantId,
            dados.Descricao,
            dados.ValorDesconto,
            dados.PercDesconto,
            dados.ValorMinimoCompra,
            dados.IdCategoria,
            dados.DataValidade,
            dados.Quantidade,
            dados.IsCupomProduto,
            dados.IdsProdutos);

        await repository.AdicionarAsync(cupom, cancellationToken);
        return cupom;
    }

    public Task<(IReadOnlyList<Cupom> Itens, int Total)> ListarAsync(
        Guid tenantId, int pagina, int tamanhoPagina, CancellationToken cancellationToken = default)
    {
        pagina = Math.Max(pagina, 1);
        tamanhoPagina = Math.Clamp(tamanhoPagina, 1, 100);

        return repository.ListarAsync(tenantId, pagina, tamanhoPagina, cancellationToken);
    }

    public Task<Cupom?> ObterPorIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
        => repository.ObterPorIdAsync(id, tenantId, cancellationToken);

    public async Task<Cupom?> AtualizarAsync(
        Guid id, Guid tenantId, CupomDados dados, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dados);

        var cupom = await repository.ObterParaAtualizacaoAsync(id, tenantId, cancellationToken);
        if (cupom is null)
        {
            return null;
        }

        cupom.Atualizar(
            dados.Descricao,
            dados.ValorDesconto,
            dados.PercDesconto,
            dados.ValorMinimoCompra,
            dados.IdCategoria,
            dados.DataValidade,
            dados.Quantidade,
            dados.IsCupomProduto,
            dados.IdsProdutos);

        await repository.AtualizarAsync(cupom, cancellationToken);
        return cupom;
    }

    public async Task<bool> RemoverAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var cupom = await repository.ObterParaAtualizacaoAsync(id, tenantId, cancellationToken);
        if (cupom is null)
        {
            return false;
        }

        await repository.RemoverAsync(cupom, cancellationToken);
        return true;
    }

    public async Task ConsumirAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var cupom = await repository.ObterParaAtualizacaoAsync(id, tenantId, cancellationToken)
            ?? throw new CupomNaoEncontradoException(id);

        try
        {
            cupom.RegistrarUso();
            await repository.AtualizarAsync(cupom, cancellationToken);
        }
        catch (Domain.Exceptions.ConcorrenciaException)
        {
            throw new Domain.Exceptions.CupomInvalidoException(
                "O cupom foi consumido por outra operação e não possui mais unidades disponíveis.");
        }
    }
}
