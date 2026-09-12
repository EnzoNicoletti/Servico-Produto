using PedidosVendas.Domain.Exceptions;

namespace PedidosVendas.Domain.Entities;

/// <summary>
/// Cupom de desconto: global, por categoria ou por produto específico.
/// Campos conforme a especificação original (com `IdCategora` corrigido para
/// `IdCategoria` e `isCupomProduto` em convenção C#), mais `TenantId` e `RowVersion`.
/// Referências a Produto/Categoria são Guids sem FK real: o Servico-Estoque ainda não
/// possui entidades Produto/Categoria (Domain vazio); o padrão Guid segue o Identity
/// (todos os Ids da plataforma são Guid). Sem navegação para fora do agregado.
/// </summary>
public sealed class Cupom
{
    private readonly List<CupomProduto> _cupomProdutos = [];

    private Cupom()
    {
        // EF Core.
        Descricao = string.Empty;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public string Descricao { get; private set; }

    public decimal ValorDesconto { get; private set; }

    public decimal PercDesconto { get; private set; }

    public decimal ValorMinimoCompra { get; private set; }

    public Guid? IdCategoria { get; private set; }

    public DateTime DataValidade { get; private set; }

    public DateTime DataCriacao { get; private set; }

    public int Quantidade { get; private set; }

    public bool IsCupomProduto { get; private set; }

    /// <summary>Token de concorrência otimista (coluna xmin do PostgreSQL).</summary>
    public uint RowVersion { get; private set; }

    public IReadOnlyCollection<CupomProduto> CupomProdutos => _cupomProdutos.AsReadOnly();

    public static Cupom Criar(
        Guid tenantId,
        string descricao,
        decimal valorDesconto,
        decimal percDesconto,
        decimal valorMinimoCompra,
        Guid? idCategoria,
        DateTime dataValidade,
        int quantidade,
        bool isCupomProduto,
        IEnumerable<Guid> idsProdutos,
        DateTime? agoraUtc = null)
    {
        var cupom = new Cupom { Id = Guid.NewGuid() };
        cupom.AplicarEstado(
            tenantId, descricao, valorDesconto, percDesconto, valorMinimoCompra,
            idCategoria, dataValidade, quantidade, isCupomProduto, idsProdutos,
            agoraUtc ?? DateTime.UtcNow, ehCriacao: true);
        cupom.DataCriacao = agoraUtc ?? DateTime.UtcNow;
        return cupom;
    }

    public void Atualizar(
        string descricao,
        decimal valorDesconto,
        decimal percDesconto,
        decimal valorMinimoCompra,
        Guid? idCategoria,
        DateTime dataValidade,
        int quantidade,
        bool isCupomProduto,
        IEnumerable<Guid> idsProdutos,
        DateTime? agoraUtc = null)
    {
        AplicarEstado(
            TenantId, descricao, valorDesconto, percDesconto, valorMinimoCompra,
            idCategoria, dataValidade, quantidade, isCupomProduto, idsProdutos,
            agoraUtc ?? DateTime.UtcNow, ehCriacao: false);
    }

    /// <summary>Consome uma unidade do cupom. Lança se esgotado.</summary>
    public void RegistrarUso()
    {
        if (Quantidade <= 0)
        {
            throw new CupomInvalidoException("Cupom esgotado: Quantidade já é zero.");
        }

        Quantidade--;
    }

    private void AplicarEstado(
        Guid tenantId,
        string descricao,
        decimal valorDesconto,
        decimal percDesconto,
        decimal valorMinimoCompra,
        Guid? idCategoria,
        DateTime dataValidade,
        int quantidade,
        bool isCupomProduto,
        IEnumerable<Guid> idsProdutos,
        DateTime agoraUtc,
        bool ehCriacao)
    {
        if (tenantId == Guid.Empty)
        {
            throw new CupomInvalidoException("TenantId é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(descricao))
        {
            throw new CupomInvalidoException("Descricao é obrigatória.");
        }

        if (valorDesconto < 0 || percDesconto < 0)
        {
            throw new CupomInvalidoException("ValorDesconto e PercDesconto não podem ser negativos.");
        }

        // Regra de domínio (roadmap 4.3): exatamente um dos dois preenchido (> 0).
        var temValor = valorDesconto > 0;
        var temPercentual = percDesconto > 0;
        if (temValor == temPercentual)
        {
            throw new CupomInvalidoException(
                "O cupom deve ter exatamente um entre ValorDesconto e PercDesconto preenchido (> 0).");
        }

        if (valorMinimoCompra < 0)
        {
            throw new CupomInvalidoException("ValorMinimoCompra não pode ser negativo.");
        }

        // Validade é data (compara-se o dia, não o instante).
        if (dataValidade.Date < agoraUtc.Date)
        {
            throw new CupomInvalidoException("DataValidade não pode estar no passado.");
        }

        if (quantidade < 0)
        {
            throw new CupomInvalidoException("Quantidade não pode ser negativa.");
        }

        var links = idsProdutos?.Distinct().ToList() ?? [];

        // Coerência entre o flag e os vínculos: produto-específico exige ≥ 1 vínculo.
        if (isCupomProduto && links.Count == 0)
        {
            throw new CupomInvalidoException(
                "Cupom marcado como por produto (isCupomProduto) exige ao menos um produto vinculado.");
        }

        if (!isCupomProduto && links.Count > 0)
        {
            throw new CupomInvalidoException(
                "Cupom global/por categoria não pode ter produtos vinculados; marque isCupomProduto.");
        }

        if (ehCriacao)
        {
            TenantId = tenantId;
        }

        Descricao = descricao.Trim();
        ValorDesconto = valorDesconto;
        PercDesconto = percDesconto;
        ValorMinimoCompra = valorMinimoCompra;
        IdCategoria = idCategoria;
        DataValidade = dataValidade;
        Quantidade = quantidade;
        IsCupomProduto = isCupomProduto;

        _cupomProdutos.Clear();
        foreach (var idProduto in links)
        {
            if (idProduto == Guid.Empty)
            {
                throw new CupomInvalidoException("IdProduto vinculado não pode ser vazio.");
            }

            _cupomProdutos.Add(new CupomProduto(Id, idProduto));
        }
    }
}
