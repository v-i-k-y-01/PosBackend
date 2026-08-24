using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PosBackend.Application.Sales;
using PosBackend.Domain.Enums;

namespace PosBackend.Api.Controllers;

/// <summary>
/// API endpoints for managing and retrieving sale transactions.
/// Accessible by both Owner and Cashier roles.
/// </summary>
[ApiController]
[Route("api/sales")]
[Authorize(Roles = "Owner,Cashier")]
public sealed class SalesController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes a new instance of the <see cref="SalesController"/> class.
    /// </summary>
    /// <param name="sender">The MediatR sender utility.</param>
    public SalesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Registers a new sale transaction. Deducts inventory stock and processes payment.
    /// </summary>
    /// <param name="body">Payload containing selected payment method and product lines.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>Details of the registered sale transaction.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(SaleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SaleDto>> Create([FromBody] CreateSaleBody body, CancellationToken cancellationToken)
    {
        var commandItems = body.Items
            .Select(item => new SaleLineRequest(item.ProductId, item.Quantity))
            .ToList();

        var saleDto = await _sender.Send(
            new CreateSaleCommand(body.PaymentMethod, commandItems),
            cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = saleDto.Id }, saleDto);
    }

    /// <summary>
    /// Retrieves a paginated list of sales history, optionally filtered by date ranges.
    /// Cashiers can only view their own sales; Owners can view all.
    /// </summary>
    /// <param name="page">The page number (1-indexed). Defaults to 1.</param>
    /// <param name="pageSize">Number of items per page. Defaults to 25.</param>
    /// <param name="from">Optional starting date boundary.</param>
    /// <param name="to">Optional ending date boundary.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A paginated page of sales DTOs.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SaleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<SaleDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var sales = await _sender.Send(
            new GetSalesQuery(page, pageSize, from, to),
            cancellationToken);

        return Ok(sales);
    }

    /// <summary>
    /// Retrieves details of a specific sale transaction by its unique identifier.
    /// Cashiers can only access sales they rung up themselves.
    /// </summary>
    /// <param name="id">The unique identifier of the sale.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>Details of the requested sale transaction.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SaleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SaleDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var sale = await _sender.Send(new GetSaleByIdQuery(id), cancellationToken);
        return Ok(sale);
    }
}

/// <summary>
/// Request contract for creating a sale transaction.
/// </summary>
/// <param name="PaymentMethod">The payment method selected by client (Cash, Card, UPI).</param>
/// <param name="Items">List of product line requests.</param>
public sealed record CreateSaleBody(PaymentMethod PaymentMethod, IReadOnlyList<SaleLineBody> Items);

/// <summary>
/// Request contract for an itemized line inside a sale request.
/// </summary>
/// <param name="ProductId">The unique identifier of the product being purchased.</param>
/// <param name="Quantity">The quantity purchased (must be positive).</param>
public sealed record SaleLineBody(Guid ProductId, int Quantity);
