namespace PedidosVendas.Domain.Enums;

/// <summary>
/// Status do Pedido. O Pedido nasce como carrinho (`Carrinho`) e evolui até a venda.
/// Persistido como inteiro (sem tabela própria — a especificação não prevê uma).
/// </summary>
public enum StatusPedido
{
    Carrinho = 1,
    AguardandoPagamento = 2,
    VendaEfetuada = 3,
    Cancelado = 4
}
