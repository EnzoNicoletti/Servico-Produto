using PedidosVendas.Domain.Enums;
using PedidosVendas.Domain.Exceptions;

namespace PedidosVendas.Domain.Entities;

/// <summary>
/// Pedido — que cumpre o papel de carrinho enquanto `Status = Carrinho` (decisão 4.1,
/// sem entidade `Carrinho` separada). `IdCliente`/`IdCupom`/`IdUnidade` nullable conforme
/// a especificação; `ValorTotal` é sempre recalculado (soma dos itens, sem desconto —
/// cupom entra na Etapa 04).
/// </summary>
public sealed class Pedido
{
    private readonly List<ProdutosPedido> _itens = [];

    private Pedido()
    {
        // EF Core.
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public DateTime DataAbertura { get; private set; }

    public Guid? IdCliente { get; private set; }

    public Guid? IdCupom { get; private set; }

    public Guid? IdUnidade { get; private set; }

    public StatusPedido Status { get; private set; }

    public DateTime? DataFechamento { get; private set; }

    public decimal ValorTotal { get; private set; }

    /// <summary>Token de concorrência otimista (coluna xmin do PostgreSQL).</summary>
    public uint RowVersion { get; private set; }

    public IReadOnlyCollection<ProdutosPedido> Itens => _itens.AsReadOnly();

    public static Pedido CriarCarrinho(
        Guid tenantId, Guid? idCliente, Guid? idUnidade, DateTime? agoraUtc = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new PedidoInvalidoException("TenantId é obrigatório.");
        }

        return new Pedido
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DataAbertura = agoraUtc ?? DateTime.UtcNow,
            IdCliente = idCliente,
            IdUnidade = idUnidade,
            Status = StatusPedido.Carrinho,
            ValorTotal = 0
        };
    }

    /// <summary>
    /// Adiciona item somando a quantidade quando o produto já está no carrinho.
    /// O preço praticado é sempre o snapshot informado (preço atual do Estoque).
    /// </summary>
    public void AdicionarItem(Guid idProduto, int quantidade, decimal valorUnitario)
    {
        if (idProduto == Guid.Empty)
        {
            throw new PedidoInvalidoException("IdProduto é obrigatório.");
        }

        if (quantidade <= 0)
        {
            throw new PedidoInvalidoException("Quantidade deve ser maior que zero.");
        }

        if (valorUnitario < 0)
        {
            throw new PedidoInvalidoException("ValorUnitario não pode ser negativo.");
        }

        GarantirCarrinhoAberto();

        var existente = _itens.FirstOrDefault(i => i.IdProduto == idProduto);
        if (existente is null)
        {
            _itens.Add(new ProdutosPedido(Id, idProduto, quantidade, valorUnitario));
        }
        else
        {
            existente.Quantidade += quantidade;
        }

        RecalcularTotal();
    }

    public void DefinirQuantidade(Guid idProduto, int novaQuantidade)
    {
        if (novaQuantidade <= 0)
        {
            throw new PedidoInvalidoException("Quantidade deve ser maior que zero.");
        }

        GarantirCarrinhoAberto();

        var existente = _itens.FirstOrDefault(i => i.IdProduto == idProduto)
            ?? throw new PedidoInvalidoException("Item não encontrado no pedido.");

        existente.Quantidade = novaQuantidade;
        RecalcularTotal();
    }

    public void RemoverItem(Guid idProduto)
    {
        GarantirCarrinhoAberto();

        var existente = _itens.FirstOrDefault(i => i.IdProduto == idProduto)
            ?? throw new PedidoInvalidoException("Item não encontrado no pedido.");

        _itens.Remove(existente);
        RecalcularTotal();
    }

    /// <summary>
    /// Vincula um cupom ao pedido (Etapa 04). Não consome quantidade aqui: o decremento
    /// ocorre só na finalização (Etapa 07), pois o carrinho pode ser abandonado.
    /// Reaplicar troca o cupom (um único IdCupom por pedido, cf. especificação).
    /// </summary>
    public void AplicarCupom(Guid idCupom)
    {
        if (idCupom == Guid.Empty)
        {
            throw new PedidoInvalidoException("IdCupom é obrigatório.");
        }

        GarantirCarrinhoAberto();
        IdCupom = idCupom;
    }

    /// <summary>Desvincula o cupom (o vínculo é "grudento": nunca removido automaticamente).</summary>
    public void RemoverCupom()
    {
        GarantirCarrinhoAberto();
        IdCupom = null;
    }

    private void GarantirCarrinhoAberto()
    {
        if (Status != StatusPedido.Carrinho)
        {
            throw new PedidoInvalidoException("Somente pedidos com status Carrinho podem ser alterados.");
        }
    }

    private void RecalcularTotal()
    {
        ValorTotal = _itens.Sum(i => i.Subtotal);
    }
}
