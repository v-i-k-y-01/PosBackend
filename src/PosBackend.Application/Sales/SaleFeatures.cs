using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PosBackend.Application.Common.Exceptions;
using PosBackend.Application.Common.Interfaces;
using PosBackend.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PosBackend.Application.Common.Exceptions;
using PosBackend.Application.Common.Interfaces;
using PosBackend.Domain.Entities;
using PosBackend.Domain.Enums;

namespace PosBackend.Application.Sales;

public sealed record SaleLineRequest(Guid ProductId, int Quantity);

public sealed record SaleItemDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal
);

public sealed record SaleDto(
    Guid Id,
    Guid CashierId,
    string CashierEmail,
    decimal TotalAmount,
    string PaymentMethod,
    DateTime CreatedAt,
    IReadOnlyList<SaleItemDto> Items
);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public sealed record CreateSaleCommand(PaymentMethod PaymentMethod, IReadOnlyList<SaleLineRequest> Items) : IRequest<SaleDto>;

public sealed record GetSalesQuery(int Page = 1, int PageSize = 25, DateTime? From = null, DateTime? To = null) : IRequest<PagedResult<SaleDto>>;

public sealed record GetSaleByIdQuery(Guid Id) : IRequest<SaleDto>;

public sealed class CreateSaleValidator : AbstractValidator<CreateSaleCommand>
{
    public CreateSaleValidator()
    {
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(items =>
        {
            items.RuleFor(i => i.ProductId).NotEmpty();
            items.RuleFor(i => i.Quantity).GreaterThan(0);
        });
    }
}

public sealed class GetSalesValidator : AbstractValidator<GetSalesQuery>
{
    public GetSalesValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From!.Value)
            .When(x => x.From.HasValue && x.To.HasValue);
    }
}

public sealed class SaleHandlers :
    IRequestHandler<CreateSaleCommand, SaleDto>,
    IRequestHandler<GetSalesQuery, PagedResult<SaleDto>>,
    IRequestHandler<GetSaleByIdQuery, SaleDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _current;

    public SaleHandlers(IAppDbContext db, ICurrentUserService current)
    {
        _db = db;
        _current = current;
    }

    private static SaleDto Map(Sale x)
    {
        var items = x.Items
            .Select(i => new SaleItemDto(
                i.ProductId,
                i.Product?.Name ?? string.Empty,
                i.Quantity,
                i.UnitPrice,
                i.Subtotal))
            .ToList();

        return new SaleDto(
            x.Id,
            x.CashierId,
            x.Cashier?.Email ?? string.Empty,
            x.TotalAmount,
            x.PaymentMethod.ToString(),
            x.CreatedAt,
            items);
    }

    public async Task<SaleDto> Handle(CreateSaleCommand request, CancellationToken cancellationToken)
    {
        return await _db.ExecuteInTransactionAsync(async token =>
        {
            // Group identical product lines by ProductId and sum quantities
            var grouped = request.Items
                .GroupBy(x => x.ProductId)
                .Select(g => new SaleLineRequest(g.Key, g.Sum(y => y.Quantity)))
                .ToList();

            var ids = grouped.Select(x => x.ProductId).ToList();

            // Load products referenced in the sale
            var products = await _db.Products
                .Where(p => ids.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, token);

            if (products.Count != ids.Count)
            {
                throw new BadRequestException("One or more products do not exist.");
            }

            // Ensure sufficient stock for each line
            foreach (var line in grouped)
            {
                var product = products[line.ProductId];
                if (product.StockQty < line.Quantity)
                    throw new BadRequestException($"Insufficient stock for '{product.Name}'.");
            }

            // Create sale and deduct stock
            var sale = new Sale
            {
                CashierId = _current.UserId,
                PaymentMethod = request.PaymentMethod,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var line in grouped)
            {
                var p = products[line.ProductId];
                p.StockQty -= line.Quantity;

                var item = new SaleItem
                {
                    ProductId = p.Id,
                    Product = p,
                    Quantity = line.Quantity,
                    UnitPrice = p.Price,
                    Subtotal = p.Price * line.Quantity
                };

                sale.Items.Add(item);
            }

            sale.TotalAmount = sale.Items.Sum(x => x.Subtotal);

            _db.Sales.Add(sale);
            await _db.SaveChangesAsync(token);

            // Load cashier navigation (use key array overload to include cancellation token)
            sale.Cashier = await _db.Users.FindAsync(new object[] { sale.CashierId }, token);

            return Map(sale);
        }, cancellationToken);
    }

    public async Task<PagedResult<SaleDto>> Handle(GetSalesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Sales
            .AsNoTracking()
            .Include(x => x.Cashier)
            .Include(x => x.Items)
                .ThenInclude(x => x.Product)
            .AsQueryable();

        if (!_current.IsOwner)
            query = query.Where(x => x.CashierId == _current.UserId);

        if (request.From.HasValue)
            query = query.Where(x => x.CreatedAt >= request.From.Value.ToUniversalTime());

        if (request.To.HasValue)
            query = query.Where(x => x.CreatedAt <= request.To.Value.ToUniversalTime());

        var total = await query.CountAsync(cancellationToken);

        var sales = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = sales.Select(Map).ToList();
        return new PagedResult<SaleDto>(dtos, request.Page, request.PageSize, total);
    }

    public async Task<SaleDto> Handle(GetSaleByIdQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Sales
            .AsNoTracking()
            .Include(x => x.Cashier)
            .Include(x => x.Items)
                .ThenInclude(x => x.Product)
            .Where(x => x.Id == request.Id);

        if (!_current.IsOwner)
            query = query.Where(x => x.CashierId == _current.UserId);

        var sale = await query.SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Sale", request.Id);

        return Map(sale);
    }
}
