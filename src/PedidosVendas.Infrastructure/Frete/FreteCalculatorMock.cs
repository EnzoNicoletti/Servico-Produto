using Microsoft.Extensions.Options;
using PedidosVendas.Application.Frete;

namespace PedidosVendas.Infrastructure.Frete;

/// <summary>
/// Implementação provisória e determinística (Etapa 05): valor = base + por-unidade ×
/// total de unidades; prazo pela faixa do primeiro dígito do CEP (0-3: 2 dias úteis,
/// 4-6: 4 dias, 7-9: 6 dias). Carrinho vazio cota zero. Ignora a unidade de origem
/// (sem endereço/CEP de Branch na plataforma hoje). Substituir por provedor real
/// (Correios/transportadora) sem tocar domínio ou chamadas — só configuração + classe.
/// </summary>
public sealed class FreteCalculatorMock(IOptions<FreteOptions> options) : IFreteCalculator
{
    public Task<CotacaoFrete> CalcularAsync(
        string cepDestino,
        IReadOnlyList<ItemFrete> itens,
        Guid? idUnidadeOrigem,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(itens);

        // Origem ignorada de forma intencional e explícita: sem endereço/CEP de Branch na
        // plataforma, não há como diferenciar por origem — null (caso comum, branch_id
        // ausente no token) ou Guid informado produzem a mesma cotação (ver teste).
        _ = idUnidadeOrigem;

        var normalizado = NormalizarCep(cepDestino);
        var config = options.Value;

        var unidades = itens.Sum(i => i.Quantidade);
        var valor = unidades == 0 ? 0 : config.ValorBase + (config.ValorPorUnidade * unidades);

        return Task.FromResult(new CotacaoFrete(normalizado, valor, PrazoPorFaixa(normalizado[0])));
    }

    public static string NormalizarCep(string cepDestino)
    {
        var digitos = new string((cepDestino ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digitos.Length != 8)
        {
            throw new CepInvalidoException(cepDestino ?? string.Empty);
        }

        return digitos;
    }

    private static int PrazoPorFaixa(char primeiroDigito) => primeiroDigito switch
    {
        >= '0' and <= '3' => 2,
        >= '4' and <= '6' => 4,
        _ => 6
    };
}
