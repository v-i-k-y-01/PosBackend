using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PosBackend.Infrastructure.Persistence;

namespace PosBackend.Api.Controllers;

/// <summary>
/// Maintenance and administrative utilities for system reset and diagnostics.
/// </summary>
[ApiController]
[Route("api/maintenance")]
public sealed class MaintenanceController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="MaintenanceController"/> class.
    /// </summary>
    public MaintenanceController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Completely wipes all data (users, stores, products, categories, sales, tokens) from the database.
    /// Re-seeds a pristine database schema.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Confirmation message.</returns>
    [HttpPost("reset-database")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetDatabase(CancellationToken cancellationToken)
    {
        // Truncate all tables with CASCADE
        await _dbContext.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE \"SaleItems\", \"Sales\", \"Products\", \"Categories\", \"Users\", \"Stores\" CASCADE;",
            cancellationToken);

        return Ok(new
        {
            success = true,
            message = "All database records (users, stores, products, categories, sales) have been completely wiped. You can now register fresh owner accounts."
        });
    }
}
