using PedidosVendas.Domain.Exceptions;

namespace PedidosVendas.Domain.Entities;

/// <summary>
/// Forma de pagamento (catálogo global, compartilhado entre tenants).
/// A distinção "à vista × parcelável" deriva de <see cref="QtdMaximaParcelas"/>:
/// igual a 1 ⇒ parcela única forçada; maior que 1 ⇒ valida 1..limite.
/// Sem hardcode de nomes ("crédito") nas regras.
/// </summary>
public sealed class FormaPagto
{
    private FormaPagto()
    {
        // EF Core.
        Descricao = string.Empty;
    }

    public FormaPagto(string descricao, int qtdMaximaParcelas)
    {
        if (string.IsNullOrWhiteSpace(descricao))
        {
            throw new FormaPagtoInvalidaException("Descricao é obrigatória.");
        }

        if (qtdMaximaParcelas < 1)
        {
            throw new FormaPagtoInvalidaException("QtdMaximaParcelas deve ser ao menos 1.");
        }

        Id = Guid.NewGuid();
        Descricao = descricao.Trim();
        QtdMaximaParcelas = qtdMaximaParcelas;
    }

    public Guid Id { get; private set; }

    public string Descricao { get; private set; }

    public int QtdMaximaParcelas { get; private set; }

    /// <summary>True quando aceita mais de 1 parcela (ex.: cartão de crédito).</summary>
    public bool PermiteParcelar => QtdMaximaParcelas > 1;
}
