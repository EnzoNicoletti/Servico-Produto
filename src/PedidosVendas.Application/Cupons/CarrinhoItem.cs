namespace PedidosVendas.Application.Cupons;

/// <summary>
/// Item de carrinho para avaliação de elegibilidade (Etapa 02: ainda não há Pedido real —
/// a Etapa 04 reaproveitará este contrato com os itens do pedido).
/// </summary>
public sealed record CarrinhoItem(
    Guid IdProduto,
    Guid? IdCategoria,
    int Quantidade,
    decimal ValorUnitario);
