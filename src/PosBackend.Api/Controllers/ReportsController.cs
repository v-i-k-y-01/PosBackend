using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PosBackend.Application.Reports;

namespace PosBackend.Api.Controllers;

/// <summary>
/// API endpoints for retrieving sales and store performance reports.
/// Access is restricted to users with the Owner role.
/// </summary>
[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Owner")]
public sealed class ReportsController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportsController"/> class.
    /// </summary>
    /// <param name="sender">The MediatR sender utility.</param>
    public ReportsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Retrieves daily revenue summary metrics, optionally filtered by a calendar date range.
    /// </summary>
    /// <param name="from">Optional starting DateOnly boundary.</param>
    /// <param name="to">Optional ending DateOnly boundary.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A list of daily revenue records.</returns>
    [HttpGet("daily-revenue")]
    [ProducesResponseType(typeof(IReadOnlyList<DailyRevenueDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DailyRevenueDto>>> Daily(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var dailyRevenue = await _sender.Send(new GetDailyRevenueQuery(from, to), cancellationToken);
        return Ok(dailyRevenue);
    }

    /// <summary>
    /// Retrieves a list of the top-performing products ranked by quantity sold.
    /// </summary>
    /// <param name="limit">The maximum number of products to return. Defaults to 10.</param>
    /// <param name="from">Optional starting DateTime boundary in UTC.</param>
    /// <param name="to">Optional ending DateTime boundary in UTC.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A list of top performing products.</returns>
    [HttpGet("top-products")]
    [ProducesResponseType(typeof(IReadOnlyList<TopProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TopProductDto>>> Top(
        [FromQuery] int limit = 10,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var topProducts = await _sender.Send(new GetTopProductsQuery(limit, from, to), cancellationToken);
        return Ok(topProducts);
    }
}
