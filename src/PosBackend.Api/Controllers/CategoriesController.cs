using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosBackend.Application.Categories;
namespace PosBackend.Api.Controllers;
[ApiController, Route("api/categories"), Authorize(Roles="Owner")]
public sealed class CategoriesController(ISender sender):ControllerBase
{
 [HttpGet] public Task<IReadOnlyList<CategoryDto>> Get(CancellationToken ct)=>sender.Send(new GetCategoriesQuery(),ct);
 [HttpGet("{id:guid}")] public Task<CategoryDto> Get(Guid id,CancellationToken ct)=>sender.Send(new GetCategoryByIdQuery(id),ct);
 [HttpPost] public async Task<ActionResult<CategoryDto>> Create(CategoryBody b,CancellationToken ct){var result=await sender.Send(new CreateCategoryCommand(b.Name),ct);return CreatedAtAction(nameof(Get),new{id=result.Id},result);}
 [HttpPut("{id:guid}")] public Task<CategoryDto> Update(Guid id,CategoryBody b,CancellationToken ct)=>sender.Send(new UpdateCategoryCommand(id,b.Name),ct);
 [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id,CancellationToken ct){await sender.Send(new DeleteCategoryCommand(id),ct);return NoContent();}
}
public sealed record CategoryBody(string Name);
