using System.ComponentModel.DataAnnotations;

namespace PedidosVendas.API.Contracts.Carrinho;

public sealed class AdicionarItemRequest
{
    [Required(ErrorMessage = "IdProduto é obrigatório.")]
    public Guid IdProduto { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantidade deve ser maior que zero.")]
    public int Quantidade { get; set; } = 1;
}
