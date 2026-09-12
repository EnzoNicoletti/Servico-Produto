namespace PedidosVendas.Application.Cupons;

/// <summary>Cupom inexistente naquele tenant (API traduz em 404).</summary>
public sealed class CupomNaoEncontradoException(Guid idCupom)
    : Exception($"Cupom {idCupom} não encontrado.");
