using System.ComponentModel.DataAnnotations;

namespace PedidosVendas.API.Contracts.Carrinho;

public sealed class AtualizarQuantidadeRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Quantidade deve ser maior que zero.")]
    public int Quantidade { get; set; }
}
