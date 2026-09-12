using PedidosVendas.Application.Pagamento;

namespace PedidosVendas.API.Contracts.Pagamento;

public sealed record FormaPagamentoResponse(
    Guid Id,
    string Descricao,
    int QtdMaximaParcelas)
{
    public static FormaPagamentoResponse From(FormaPagamentoItem item) => new(
        item.Id, item.Descricao, item.QtdMaximaParcelas);
}

public sealed record ParcelamentoResponse(
    Guid IdFormaPagto,
    string Descricao,
    int QuantidadeParcelas)
{
    public static ParcelamentoResponse From(ParcelamentoValidado validado) => new(
        validado.IdFormaPagto, validado.Descricao, validado.QuantidadeParcelas);
}
