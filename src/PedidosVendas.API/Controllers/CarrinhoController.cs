using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PedidosVendas.API.Contracts.Carrinho;
using PedidosVendas.API.Contracts.Cupons;
using PedidosVendas.Application.Carrinho;
using PedidosVendas.Application.Common.Interfaces;
using PedidosVendas.Application.Cupons;
using PedidosVendas.Application.Frete;
using PedidosVendas.Domain.Exceptions;

namespace PedidosVendas.API.Controllers;

/// <summary>
/// Carrinho (`Pedido` com `Status = Carrinho`). Exige autenticação: o carrinho é sempre
/// localizado por (TenantId, IdCliente) do JWT — nunca do payload (decisão #4).
/// Sem JWT → 401; fora do escopo usuário/tenant → 404.
/// Etapa 04: aplicar/remover cupom e consultar o desconto (recalculado sempre).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/carrinho")]
public sealed class CarrinhoController(
    ICarrinhoService carrinho,
    ICupomAplicacaoService cupons,
    IFreteCalculator frete,
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

    [HttpPost("cupom")]
    [ProducesResponseType(typeof(CarrinhoComDescontoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CarrinhoComDescontoResponse>> AplicarCupom(
        [FromBody] AplicarCupomRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var resultado = await cupons.AplicarCupomAsync(
                tenant.TenantId, usuario.UserId, tenant.BranchId,
                request.IdCupom, DateTime.UtcNow, cancellationToken);

            if (resultado is null)
            {
                return NotFound();
            }

            return Ok(new CarrinhoComDescontoResponse(
                CarrinhoResponse.From(resultado.Value.Pedido),
                DescontoResponse.From(resultado.Value.Desconto)));
        }
        catch (CupomNaoEncontradoException)
        {
            return NotFound();
        }
        catch (CupomInaplicavelException exception)
        {
            return ValidationProblem(detail: exception.Message);
        }
        catch (PedidoInvalidoException exception)
        {
            return ValidationProblem(detail: exception.Message);
        }
    }

    [HttpDelete("cupom")]
    [ProducesResponseType(typeof(CarrinhoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CarrinhoResponse>> RemoverCupom(CancellationToken cancellationToken)
    {
        try
        {
            var pedido = await cupons.RemoverCupomAsync(
                tenant.TenantId, usuario.UserId, tenant.BranchId, cancellationToken);
            return pedido is null ? NotFound() : Ok(CarrinhoResponse.From(pedido));
        }
        catch (PedidoInvalidoException exception)
        {
            return ValidationProblem(detail: exception.Message);
        }
    }

    [HttpGet("desconto")]
    [ProducesResponseType(typeof(DescontoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DescontoResponse>> ObterDesconto(CancellationToken cancellationToken)
    {
        var resumo = await cupons.CalcularDescontoAsync(
            tenant.TenantId, usuario.UserId, tenant.BranchId, DateTime.UtcNow, cancellationToken);
        return resumo is null ? NotFound() : Ok(DescontoResponse.From(resumo));
    }

    [HttpPost("frete")]
    [ProducesResponseType(typeof(FreteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<FreteResponse>> CalcularFrete(
        [FromBody] CalcularFreteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var pedido = await carrinho.ObterAsync(
                tenant.TenantId, usuario.UserId, tenant.BranchId, cancellationToken);
            if (pedido is null)
            {
                return NotFound();
            }

            var itens = pedido.Itens
                .Select(i => new ItemFrete(i.IdProduto, i.Quantidade, i.ValorUnitario))
                .ToList();

            var cotacao = await frete.CalcularAsync(
                request.CepDestino, itens, pedido.IdUnidade, cancellationToken);
            return Ok(FreteResponse.From(cotacao));
        }
        catch (CepInvalidoException exception)
        {
            return ValidationProblem(detail: exception.Message);
        }
    }
}
