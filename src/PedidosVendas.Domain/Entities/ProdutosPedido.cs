using PedidosVendas.Domain.Exceptions;

namespace PedidosVendas.Domain.Entities;

/// <summary>
/// Item do pedido/carrinho. `IdPedido` (decisão 4.1: `Pedido` cumpre o papel de carrinho).
/// `ValorUnitario` é snapshot do preço no momento da adição (decisão 4.8), obtido via
/// API do Estoque — protege o `ValorTotal` contra mudanças de preço.
/// </summary>
public sealed class ProdutosPedido
{
    private ProdutosPedido()
    {
        // EF Core.
    }

    public ProdutosPedido(Guid idPedido, Guid idProduto, int quantidade, decimal valorUnitario)
    {
        if (idPedido == Guid.Empty)
        {
            throw new PedidoInvalidoException("IdPedido é obrigatório.");
        }

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

        Id = Guid.NewGuid();
        IdPedido = idPedido;
        IdProduto = idProduto;
        Quantidade = quantidade;
        ValorUnitario = valorUnitario;
    }

    public Guid Id { get; private set; }

    public Guid IdPedido { get; private set; }

    public Guid IdProduto { get; private set; }

    public int Quantidade { get; internal set; }

    public decimal ValorUnitario { get; private set; }

    public Pedido Pedido { get; private set; } = null!;

    public decimal Subtotal => Quantidade * ValorUnitario;
}
