namespace PedidosVendas.API.Contracts;

/// <summary>
/// Envelope de listagem paginada (a API dos serviços de referência retorna DTOs
/// diretos; para listas paginadas adota-se este envelope mínimo e explícito).
/// </summary>
public sealed record PagedResult<T>(
    int Total,
    int Pagina,
    int TamanhoPagina,
    IReadOnlyList<T> Itens);
