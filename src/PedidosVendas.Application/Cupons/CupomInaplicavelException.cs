namespace PedidosVendas.Application.Cupons;

/// <summary>Cupom existente mas não aplicável ao carrinho atual (API traduz em 400).</summary>
public sealed class CupomInaplicavelException(string motivo) : Exception(motivo);
