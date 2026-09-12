namespace PedidosVendas.Application.Frete;

/// <summary>CEP de destino ausente ou fora do formato brasileiro de 8 dígitos.</summary>
public sealed class CepInvalidoException(string cep)
    : Exception($"CEP de destino inválido: '{cep}'. Informe 8 dígitos (ex.: 01310-100).");
