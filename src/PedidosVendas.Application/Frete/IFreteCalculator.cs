namespace PedidosVendas.Application.Frete;

/// <summary>
/// Calcula frete para o checkout. O domínio nunca referencia implementações concretas —
/// a troca de provedor (mock → Correios/transportadora) é só configuração + nova classe.
/// `idUnidadeOrigem` é aceito para uso futuro (a `Branch` do Identity não tem endereço/CEP
/// hoje, então o mock o ignora de forma documentada).
/// </summary>
public interface IFreteCalculator
{
    Task<CotacaoFrete> CalcularAsync(
        string cepDestino,
        IReadOnlyList<ItemFrete> itens,
        Guid? idUnidadeOrigem,
        CancellationToken cancellationToken = default);
}
