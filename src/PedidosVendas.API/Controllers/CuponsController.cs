using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PedidosVendas.API.Contracts;
using PedidosVendas.API.Contracts.Cupons;
using PedidosVendas.Application.Common.Interfaces;
using PedidosVendas.Application.Cupons;
using PedidosVendas.Domain.Exceptions;

namespace PedidosVendas.API.Controllers;

/// <summary>
/// CRUD de Cupom (Etapa 02) — sempre no escopo do TenantId do JWT.
/// Sem JWT válido → 401; fora do tenant → 404 (sem vazar existência).
/// A aplicação do cupom a um Pedido real é escopo da Etapa 04.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/cupons")]
public sealed class CuponsController(ICupomService cupons, ITenantContext tenant) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(CupomResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CupomResponse>> Criar(
        [FromBody] CupomRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var cupom = await cupons.CriarAsync(tenant.TenantId, ParaDados(request), cancellationToken);
            return CreatedAtAction(nameof(ObterPorId), new { id = cupom.Id }, CupomResponse.From(cupom));
        }
        catch (CupomInvalidoException exception)
        {
            return ValidationProblem(detail: exception.Message);
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CupomResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<CupomResponse>>> Listar(
        [FromQuery] int pagina = 1, [FromQuery] int tamanhoPagina = 20,
        CancellationToken cancellationToken = default)
    {
        var (itens, total) = await cupons.ListarAsync(tenant.TenantId, pagina, tamanhoPagina, cancellationToken);

        pagina = Math.Max(pagina, 1);
        tamanhoPagina = Math.Clamp(tamanhoPagina, 1, 100);

        return Ok(new PagedResult<CupomResponse>(
            total, pagina, tamanhoPagina, itens.Select(CupomResponse.From).ToList()));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CupomResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CupomResponse>> ObterPorId(
        Guid id, CancellationToken cancellationToken)
    {
        var cupom = await cupons.ObterPorIdAsync(id, tenant.TenantId, cancellationToken);
        return cupom is null ? NotFound() : Ok(CupomResponse.From(cupom));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CupomResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CupomResponse>> Atualizar(
        Guid id, [FromBody] CupomRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var cupom = await cupons.AtualizarAsync(id, tenant.TenantId, ParaDados(request), cancellationToken);
            return cupom is null ? NotFound() : Ok(CupomResponse.From(cupom));
        }
        catch (CupomInvalidoException exception)
        {
            return ValidationProblem(detail: exception.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Remover(Guid id, CancellationToken cancellationToken)
    {
        var removido = await cupons.RemoverAsync(id, tenant.TenantId, cancellationToken);
        return removido ? NoContent() : NotFound();
    }

    private static CupomDados ParaDados(CupomRequest request) => new(
        request.Descricao,
        request.ValorDesconto,
        request.PercDesconto,
        request.ValorMinimoCompra,
        request.IdCategoria,
        request.DataValidade,
        request.Quantidade,
        request.IsCupomProduto,
        request.IdsProdutos);
}
