using System.ComponentModel.DataAnnotations;

namespace PedidosVendas.API.Contracts.Cupons;

public sealed class AplicarCupomRequest
{
    [Required(ErrorMessage = "IdCupom é obrigatório.")]
    public Guid IdCupom { get; set; }
}
