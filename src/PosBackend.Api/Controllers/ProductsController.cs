using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PosBackend.Application.Products;

namespace PosBackend.Api.Controllers;

/// <summary>
/// API endpoints for managing shop products.
/// Authentication is required. Owners can manage inventory; Cashiers can list and search products.
/// </summary>
[ApiController]
[Route("api/products")]
[Authorize]
public sealed class ProductsController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductsController"/> class.
    /// </summary>
    /// <param name="sender">The MediatR sender utility.</param>
    public ProductsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Retrieves a list of products, optionally filtered by category and SKU/name search terms.
    /// Access is permitted for both Owner and Cashier roles.
    /// </summary>
    /// <param name="categoryId">Optional category filter.</param>
    /// <param name="search">Optional search term for product name or SKU.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A list of products.</returns>
    [HttpGet]
    [Authorize(Roles = "Owner,Cashier")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> Get(
        [FromQuery] Guid? categoryId,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var products = await _sender.Send(new GetProductsQuery(categoryId, search), cancellationToken);
        return Ok(products);
    }

    /// <summary>
    /// Retrieves details of a specific product by its identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the product.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The product details.</returns>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Owner,Cashier")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var product = await _sender.Send(new GetProductByIdQuery(id), cancellationToken);
        return Ok(product);
    }

    /// <summary>
    /// Retrieves details of a specific product by its exact barcode or SKU string.
    /// Access is permitted for both Owner and Cashier roles.
    /// </summary>
    /// <param name="barcode">The barcode or SKU string to query.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The product details.</returns>
    [HttpGet("barcode/{barcode}")]
    [Authorize(Roles = "Owner,Cashier")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetByBarcode(string barcode, CancellationToken cancellationToken)
    {
        var product = await _sender.Send(new GetProductByBarcodeQuery(barcode), cancellationToken);
        return Ok(product);
    }

    /// <summary>
    /// Generates a unique, standard 12-digit barcode guaranteed not to conflict with existing catalog items.
    /// Access is restricted to users with the Owner role.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The generated barcode value.</returns>
    [HttpGet("generate-barcode")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(GeneratedBarcodeDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<GeneratedBarcodeDto>> GenerateBarcode(CancellationToken cancellationToken)
    {
        var barcode = await _sender.Send(new GenerateBarcodeQuery(), cancellationToken);
        return Ok(barcode);
    }

    /// <summary>
    /// Creates a new product in the store inventory.
    /// Access is restricted to users with the Owner role.
    /// </summary>
    /// <param name="body">Payload containing product property parameters.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The newly created product details.</returns>
    [HttpPost]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDto>> Create([FromBody] ProductBody body, CancellationToken cancellationToken)
    {
        var product = await _sender.Send(
            new CreateProductCommand(body.CategoryId, body.Name, body.Sku, body.Price, body.StockQty),
            cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = product.Id }, product);
    }

    /// <summary>
    /// Updates the properties of an existing product.
    /// Access is restricted to users with the Owner role.
    /// </summary>
    /// <param name="id">The unique identifier of the product to update.</param>
    /// <param name="body">Payload containing the new product property parameters.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The updated product details.</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDto>> Update(Guid id, [FromBody] ProductBody body, CancellationToken cancellationToken)
    {
        var updatedProduct = await _sender.Send(
            new UpdateProductCommand(id, body.CategoryId, body.Name, body.Sku, body.Price, body.StockQty),
            cancellationToken);

        return Ok(updatedProduct);
    }

    /// <summary>
    /// Deletes a product from the inventory by its unique identifier.
    /// Access is restricted to users with the Owner role.
    /// </summary>
    /// <param name="id">The unique identifier of the product to delete.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>NoContent status upon successful deletion.</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteProductCommand(id), cancellationToken);
        return NoContent();
    }
}

/// <summary>
/// Request body payload for product operations.
/// </summary>
/// <param name="CategoryId">Optional category ID to assign product to.</param>
/// <param name="Name">Display name of the product.</param>
/// <param name="Sku">Unique Stock Keeping Unit barcode code.</param>
/// <param name="Price">Product price.</param>
/// <param name="StockQty">Product starting stock count.</param>
public sealed record ProductBody(Guid? CategoryId, string Name, string Sku, decimal Price, int StockQty);
