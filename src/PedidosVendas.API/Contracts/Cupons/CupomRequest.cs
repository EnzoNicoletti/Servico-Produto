using System.ComponentModel.DataAnnotations;

namespace PedidosVendas.API.Contracts.Cupons;

/// <summary>
/// Payload de criação/atualização de cupom. Sem TenantId (sempre do contexto autenticado).
/// A regra "exatamente um desconto" é validada no domínio (400 se violada).
/// </summary>
public sealed class CupomRequest
{
    [Required(ErrorMessage = "Descricao é obrigatória.")]
    [MaxLength(200)]
    public string Descricao { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "ValorDesconto não pode ser negativo.")]
    public decimal ValorDesconto { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "PercDesconto não pode ser negativo.")]
    public decimal PercDesconto { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "ValorMinimoCompra não pode ser negativo.")]
    public decimal ValorMinimoCompra { get; set; }

    public Guid? IdCategoria { get; set; }

    [Required(ErrorMessage = "DataValidade é obrigatória.")]
    public DateTime DataValidade { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Quantidade não pode ser negativa.")]
    public int Quantidade { get; set; }

    public bool IsCupomProduto { get; set; }

    public List<Guid> IdsProdutos { get; set; } = [];
}
