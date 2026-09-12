using System.ComponentModel.DataAnnotations;

namespace PedidosVendas.API.Contracts.Carrinho;

public sealed class CalcularFreteRequest
{
    [Required(ErrorMessage = "CepDestino é obrigatório.")]
    public string CepDestino { get; set; } = string.Empty;
}
