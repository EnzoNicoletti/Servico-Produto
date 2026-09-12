using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PedidosVendas.API.Contracts.Carrinho;
using PedidosVendas.Application.Carrinho;
using PedidosVendas.Application.Common.Interfaces;
using PedidosVendas.Domain.Exceptions;

namespace PedidosVendas.API.Controllers;

/// <summary>
/// Carrinho (`Pedido` com `Status = Carrinho`). Exige autenticação: o carrinho é sempre
/// localizado por (TenantId, IdCliente) do JWT — nunca do payload (decisão #4).
/// Sem JWT → 401; fora do escopo usuário/tenant → 404.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/carrinho")]
public sealed class CarrinhoController(
    ICarrinhoService carrinho,
    ICurrentUserService usuario,
    ITenantContext tenant) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(CarrinhoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CarrinhoResponse>> ObterOuCriar(CancellationToken cancellationToken)
    {
        var pedido = await carrinho.ObterOuCriarAsync(
            tenant.TenantId, usuario.UserId, tenant.BranchId, cancellationToken);
        return Ok(CarrinhoResponse.From(pedido));
    }

    [HttpPost("itens")]
    [ProducesResponseType(typeof(CarrinhoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CarrinhoResponse>> AdicionarItem(
        [FromBody] AdicionarItemRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var pedido = await carrinho.AdicionarItemAsync(
                tenant.TenantId, usuario.UserId, tenant.BranchId,
                request.IdProduto, request.Quantidade, cancellationToken);
            return Ok(CarrinhoResponse.From(pedido));
        }
        catch (ProdutoNaoEncontradoException exception)
        {
            return NotFound(new ProblemDetails { Title = "Produto não encontrado", Detail = exception.Message });
        }
        catch (ProdutoIndisponivelException exception)
        {
            return ValidationProblem(detail: exception.Message);
        }
        catch (PedidoInvalidoException exception)
        {
            return ValidationProblem(detail: exception.Message);
        }
    }

    [HttpPut("itens/{idProduto:guid}")]
    [ProducesResponseType(typeof(CarrinhoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CarrinhoResponse>> DefinirQuantidade(
        Guid idProduto, [FromBody] AtualizarQuantidadeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var pedido = await carrinho.DefinirQuantidadeAsync(
                tenant.TenantId, usuario.UserId, tenant.BranchId,
                idProduto, request.Quantidade, cancellationToken);
            return pedido is null ? NotFound() : Ok(CarrinhoResponse.From(pedido));
        }
        catch (PedidoInvalidoException exception)
        {
            return ValidationProblem(detail: exception.Message);
        }
    }

    [HttpDelete("itens/{idProduto:guid}")]
    [ProducesResponseType(typeof(CarrinhoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CarrinhoResponse>> RemoverItem(
        Guid idProduto, CancellationToken cancellationToken)
    {
        try
        {
            var pedido = await carrinho.RemoverItemAsync(
                tenant.TenantId, usuario.UserId, tenant.BranchId,
                idProduto, cancellationToken);
            return pedido is null ? NotFound() : Ok(CarrinhoResponse.From(pedido));
        }
        catch (PedidoInvalidoException exception)
        {
            return ValidationProblem(detail: exception.Message);
        }
    }
}
