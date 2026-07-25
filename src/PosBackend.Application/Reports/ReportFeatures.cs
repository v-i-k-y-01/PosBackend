using MediatR;
using Microsoft.EntityFrameworkCore;
using PosBackend.Application.Common.Interfaces;

namespace PosBackend.Application.Reports;
public sealed record DailyRevenueDto(DateOnly Date, int SaleCount, decimal TotalRevenue);
public sealed record TopProductDto(Guid ProductId, string ProductName, int QuantitySold, decimal Revenue);
public sealed record GetDailyRevenueQuery(DateOnly? From = null, DateOnly? To = null) : IRequest<IReadOnlyList<DailyRevenueDto>>;
public sealed record GetTopProductsQuery(int Limit = 10, DateTime? From = null, DateTime? To = null) : IRequest<IReadOnlyList<TopProductDto>>;
public sealed class ReportHandlers(IAppDbContext db) : IRequestHandler<GetDailyRevenueQuery,IReadOnlyList<DailyRevenueDto>>, IRequestHandler<GetTopProductsQuery,IReadOnlyList<TopProductDto>>
{
 public async Task<IReadOnlyList<DailyRevenueDto>> Handle(GetDailyRevenueQuery r,CancellationToken ct){var q=db.Sales.AsNoTracking().AsQueryable();if(r.From.HasValue){var f=r.From.Value.ToDateTime(TimeOnly.MinValue,DateTimeKind.Utc);q=q.Where(x=>x.CreatedAt>=f);}if(r.To.HasValue){var t=r.To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue,DateTimeKind.Utc);q=q.Where(x=>x.CreatedAt<t);}return await q.GroupBy(x=>DateOnly.FromDateTime(x.CreatedAt)).OrderBy(x=>x.Key).Select(x=>new DailyRevenueDto(x.Key,x.Count(),x.Sum(y=>y.TotalAmount))).ToListAsync(ct);}
 public async Task<IReadOnlyList<TopProductDto>> Handle(GetTopProductsQuery r,CancellationToken ct)
 {
  var q = db.SaleItems.AsNoTracking().AsQueryable();
  if (r.From.HasValue)
  {
    q = q.Where(x => x.Sale!.CreatedAt >= r.From.Value.ToUniversalTime());
  }
  if (r.To.HasValue)
  {
    q = q.Where(x => x.Sale!.CreatedAt <= r.To.Value.ToUniversalTime());
  }

  var aggregated = await q.GroupBy(x => x.ProductId)
    .Select(g => new
    {
      ProductId = g.Key,
      QuantitySold = g.Sum(y => y.Quantity),
      Revenue = g.Sum(y => y.Subtotal)
    })
    .OrderByDescending(x => x.QuantitySold)
    .Take(Math.Clamp(r.Limit, 1, 100))
    .ToListAsync(ct);

  var names = await db.Products.AsNoTracking()
    .Where(p => aggregated.Select(x => x.ProductId).Contains(p.Id))
    .ToDictionaryAsync(p => p.Id, p => p.Name, ct);

  return aggregated
    .Select(x => new TopProductDto(x.ProductId, names.GetValueOrDefault(x.ProductId) ?? string.Empty, x.QuantitySold, x.Revenue))
    .OrderByDescending(x => x.QuantitySold)
    .ThenBy(x => x.ProductName)
    .ToList();
 }
}
