using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosBackend.Application.Sales;
using PosBackend.Domain.Enums;
namespace PosBackend.Api.Controllers;
[ApiController, Route("api/sales"), Authorize(Roles="Owner,Cashier")]
public sealed class SalesController(ISender sender):ControllerBase
{
 [HttpPost] public async Task<ActionResult<SaleDto>> Create(CreateSaleBody b,CancellationToken ct){var x=await sender.Send(new CreateSaleCommand(b.PaymentMethod,b.Items.Select(i=>new SaleLineRequest(i.ProductId,i.Quantity)).ToList()),ct);return CreatedAtAction(nameof(Get),new{id=x.Id},x);}
 [HttpGet] public Task<PagedResult<SaleDto>> List([FromQuery]int page=1,[FromQuery]int pageSize=25,[FromQuery]DateTime? from=null,[FromQuery]DateTime? to=null,CancellationToken ct=default)=>sender.Send(new GetSalesQuery(page,pageSize,from,to),ct);
 [HttpGet("{id:guid}")] public Task<SaleDto> Get(Guid id,CancellationToken ct)=>sender.Send(new GetSaleByIdQuery(id),ct);
}
public sealed record CreateSaleBody(PaymentMethod PaymentMethod,IReadOnlyList<SaleLineBody> Items);
public sealed record SaleLineBody(Guid ProductId,int Quantity);
