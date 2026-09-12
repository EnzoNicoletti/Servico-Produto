namespace PedidosVendas.Application.Pagamento;

/// <summary>Forma de pagamento com o limite já resolvido para o Tenant.</summary>
public sealed record FormaPagamentoItem(
    Guid Id,
    string Descricao,
    int QtdMaximaParcelas);

/// <summary>Parcelamento normalizado após validação (Etapa 07 reutiliza).</summary>
public sealed record ParcelamentoValidado(
    Guid IdFormaPagto,
    string Descricao,
    int QuantidadeParcelas);
