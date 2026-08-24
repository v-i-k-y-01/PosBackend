using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PosBackend.Application.Categories;

namespace PosBackend.Api.Controllers;

/// <summary>
/// API endpoints for managing catalog categories.
/// Access is restricted to users with the Owner role.
/// </summary>
[ApiController]
[Route("api/categories")]
[Authorize(Roles = "Owner")]
public sealed class CategoriesController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoriesController"/> class.
    /// </summary>
    /// <param name="sender">The MediatR sender utility.</param>
    public CategoriesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Retrieves a list of all categories sorted alphabetically.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A list of category DTOs.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> Get(CancellationToken cancellationToken)
    {
        var categories = await _sender.Send(new GetCategoriesQuery(), cancellationToken);
        return Ok(categories);
    }

    /// <summary>
    /// Retrieves details of a specific category by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the category.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The category details.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var category = await _sender.Send(new GetCategoryByIdQuery(id), cancellationToken);
        return Ok(category);
    }

    /// <summary>
    /// Creates a new category in the system catalog.
    /// </summary>
    /// <param name="body">Payload containing the category name.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The newly created category details.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> Create([FromBody] CategoryBody body, CancellationToken cancellationToken)
    {
        var category = await _sender.Send(new CreateCategoryCommand(body.Name), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = category.Id }, category);
    }

    /// <summary>
    /// Updates the properties of an existing category.
    /// </summary>
    /// <param name="id">The unique identifier of the category to update.</param>
    /// <param name="body">Payload containing the new category properties.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The updated category details.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> Update(Guid id, [FromBody] CategoryBody body, CancellationToken cancellationToken)
    {
        var updatedCategory = await _sender.Send(new UpdateCategoryCommand(id, body.Name), cancellationToken);
        return Ok(updatedCategory);
    }

    /// <summary>
    /// Deletes a category by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the category to delete.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>NoContent status upon successful deletion.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteCategoryCommand(id), cancellationToken);
        return NoContent();
    }
}

/// <summary>
/// Request body payload for category operations.
/// </summary>
/// <param name="Name">The category display name.</param>
public sealed record CategoryBody(string Name);
