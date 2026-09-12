namespace PedidosVendas.Domain.Entities;

/// <summary>
/// Vínculo entre um <see cref="Cupom"/> por produto e um produto do Estoque
/// (referência por Guid, sem FK real — ver <see cref="Cupom"/>).
/// </summary>
public sealed class CupomProduto
{
    private CupomProduto()
    {
        // EF Core.
    }

    public CupomProduto(Guid idCupom, Guid idProduto)
    {
        Id = Guid.NewGuid();
        IdCupom = idCupom;
        IdProduto = idProduto;
    }

    public Guid Id { get; private set; }

    public Guid IdCupom { get; private set; }

    public Guid IdProduto { get; private set; }

    public Cupom Cupom { get; private set; } = null!;
}
