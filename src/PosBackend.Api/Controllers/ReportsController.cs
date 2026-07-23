using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosBackend.Application.Reports;
namespace PosBackend.Api.Controllers;
[ApiController, Route("api/reports"), Authorize(Roles="Owner")]
public sealed class ReportsController(ISender sender):ControllerBase
{
 [HttpGet("daily-revenue")] public Task<IReadOnlyList<DailyRevenueDto>> Daily([FromQuery]DateOnly? from,[FromQuery]DateOnly? to,CancellationToken ct)=>sender.Send(new GetDailyRevenueQuery(from,to),ct);
 [HttpGet("top-products")] public Task<IReadOnlyList<TopProductDto>> Top([FromQuery]int limit=10,[FromQuery]DateTime? from=null,[FromQuery]DateTime? to=null,CancellationToken ct=default)=>sender.Send(new GetTopProductsQuery(limit,from,to),ct);
}
