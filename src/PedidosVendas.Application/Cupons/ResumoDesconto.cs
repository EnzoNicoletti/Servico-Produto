namespace PedidosVendas.Application.Cupons;

/// <summary>Desconto de um item do carrinho (rateio proporcional do desconto fixo).</summary>
public sealed record ItemDesconto(
    Guid IdProduto,
    int Quantidade,
    decimal ValorUnitario,
    decimal Desconto);

/// <summary>
/// Resumo do desconto do carrinho: calculado sob demanda a cada leitura (nunca persistido —
/// roadmap 6.2), exibido na UI e reutilizado na finalização (Etapa 07).
/// </summary>
public sealed record ResumoDesconto(
    bool Aplicavel,
    string? Motivo,
    decimal ValorDesconto,
    decimal SubtotalElegivel,
    IReadOnlyList<ItemDesconto> Itens)
{
    public static ResumoDesconto SemCupom(string motivo) =>
        new(false, motivo, 0, 0, []);
}
