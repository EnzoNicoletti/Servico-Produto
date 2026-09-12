using System.ComponentModel.DataAnnotations;

namespace PedidosVendas.API.Contracts.Pagamento;

public sealed class ValidarParcelasRequest
{
    [Required(ErrorMessage = "IdFormaPagto é obrigatório.")]
    public Guid IdFormaPagto { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "QuantidadeParcelas deve ser ao menos 1.")]
    public int QuantidadeParcelas { get; set; } = 1;
}
