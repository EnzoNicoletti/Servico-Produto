using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PedidosVendas.API.Contracts.Pagamento;
using PedidosVendas.Application.Common.Interfaces;
using PedidosVendas.Application.Pagamento;

namespace PedidosVendas.API.Controllers;

/// <summary>
/// Catálogo de formas de pagamento (global) com limites resolvidos para o Tenant
/// + validação de parcelas (prévia do checkout; Etapa 07 reutiliza o serviço).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/formas-pagamento")]
public sealed class FormasPagamentoController(
    IParcelamentoService parcelas,
    ITenantContext tenant) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FormaPagamentoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<FormaPagamentoResponse>>> Listar(
        CancellationToken cancellationToken)
    {
        var itens = await parcelas.ListarAsync(tenant.TenantId, cancellationToken);
        return Ok(itens.Select(FormaPagamentoResponse.From).ToList());
    }

    [HttpPost("validar-parcelas")]
    [ProducesResponseType(typeof(ParcelamentoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ParcelamentoResponse>> ValidarParcelas(
        [FromBody] ValidarParcelasRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var validado = await parcelas.ValidarAsync(
                tenant.TenantId, request.IdFormaPagto, request.QuantidadeParcelas, cancellationToken);
            return Ok(ParcelamentoResponse.From(validado));
        }
        catch (FormaPagtoNaoEncontradaException)
        {
            return NotFound();
        }
        catch (ParcelamentoInvalidoException exception)
        {
            return ValidationProblem(detail: exception.Message);
        }
    }
}
