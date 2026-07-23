using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosBackend.Application.Products;
namespace PosBackend.Api.Controllers;
[ApiController, Route("api/products"), Authorize]
public sealed class ProductsController(ISender sender):ControllerBase
{
 [HttpGet, Authorize(Roles="Owner,Cashier")] public Task<IReadOnlyList<ProductDto>> Get([FromQuery]Guid? categoryId,[FromQuery]string? search,CancellationToken ct)=>sender.Send(new GetProductsQuery(categoryId,search),ct);
 [HttpGet("{id:guid}"), Authorize(Roles="Owner,Cashier")] public Task<ProductDto> Get(Guid id,CancellationToken ct)=>sender.Send(new GetProductByIdQuery(id),ct);
 [HttpPost, Authorize(Roles="Owner")] public async Task<ActionResult<ProductDto>> Create(ProductBody b,CancellationToken ct){var x=await sender.Send(new CreateProductCommand(b.CategoryId,b.Name,b.Sku,b.Price,b.StockQty),ct);return CreatedAtAction(nameof(Get),new{id=x.Id},x);}
 [HttpPut("{id:guid}"), Authorize(Roles="Owner")] public Task<ProductDto> Update(Guid id,ProductBody b,CancellationToken ct)=>sender.Send(new UpdateProductCommand(id,b.CategoryId,b.Name,b.Sku,b.Price,b.StockQty),ct);
 [HttpDelete("{id:guid}"), Authorize(Roles="Owner")] public async Task<IActionResult> Delete(Guid id,CancellationToken ct){await sender.Send(new DeleteProductCommand(id),ct);return NoContent();}
}
public sealed record ProductBody(Guid? CategoryId,string Name,string Sku,decimal Price,int StockQty);
