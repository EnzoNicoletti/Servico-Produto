namespace PedidosVendas.Application.Frete;

/// <summary>
/// Configuração do frete (`Frete`). `Provedor` seleciona a implementação
/// ("Mock" nesta etapa; ex.: "Correios" no futuro) sem alterar código de chamada.
/// </summary>
public sealed class FreteOptions
{
    public const string SectionName = "Frete";

    public string Provedor { get; set; } = "Mock";

    public decimal ValorBase { get; set; } = 12.00m;

    public decimal ValorPorUnidade { get; set; } = 2.00m;
}
