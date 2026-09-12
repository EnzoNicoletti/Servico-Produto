namespace PedidosVendas.Domain.Exceptions;

/// <summary>
/// Conflito de concorrência otimista (RowVersion/xmin): o registro foi alterado por outra
/// operação entre a leitura e a gravação. Lançada pela Infrastructure (tradução de
/// DbUpdateConcurrencyException, sem vazar tipos do EF); a Application traduz em erro de
/// negócio claro. Não atravessa a API como 500.
/// </summary>
public sealed class ConcorrenciaException(string message) : Exception(message);
