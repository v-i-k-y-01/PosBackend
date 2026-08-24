using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PosBackend.Application.Common.Exceptions;
using PosBackend.Application.Common.Interfaces;
using PosBackend.Domain.Entities;
using PosBackend.Domain.Enums;

namespace PosBackend.Application.Sales;

/// <summary>
/// Represents a requested line item in a sale creation command.
/// </summary>
/// <param name="ProductId">Unique identifier of the product being purchased.</param>
/// <param name="Quantity">The quantity of the product being purchased.</param>
public sealed record SaleLineRequest(Guid ProductId, int Quantity);

/// <summary>
/// Data transfer object representing an itemized line inside a sale transaction.
/// </summary>
/// <param name="ProductId">Unique identifier of the sold product.</param>
/// <param name="ProductName">Name of the sold product.</param>
/// <param name="Quantity">Quantity of units sold.</param>
/// <param name="UnitPrice">The historical unit price snapshot at time of checkout.</param>
/// <param name="Subtotal">The calculated subtotal (Quantity * UnitPrice).</param>
public sealed record SaleItemDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal
);

/// <summary>
/// Data transfer object carrying complete details of a sale transaction.
/// </summary>
/// <param name="Id">Unique identifier of the sale.</param>
/// <param name="CashierId">Identifier of the cashier user who rung up the sale.</param>
/// <param name="CashierEmail">Email of the cashier user.</param>
/// <param name="TotalAmount">Total transactional amount of the sale.</param>
/// <param name="PaymentMethod">The payment method used (Cash, Card, UPI).</param>
/// <param name="CreatedAt">The timestamp in UTC when the sale occurred.</param>
/// <param name="Items">List of itemized lines included in this sale.</param>
public sealed record SaleDto(
    Guid Id,
    Guid CashierId,
    string CashierEmail,
    decimal TotalAmount,
    string PaymentMethod,
    DateTime CreatedAt,
    IReadOnlyList<SaleItemDto> Items
);

/// <summary>
/// Represents a generic structure for carrying paginated results.
/// </summary>
/// <typeparam name="T">Type of elements contained in the paginated page.</typeparam>
/// <param name="Items">Collection of items for the current page.</param>
/// <param name="Page">The current 1-indexed page number.</param>
/// <param name="PageSize">Max number of items returned per page.</param>
/// <param name="TotalCount">Total count of items matching the query in the system.</param>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

/// <summary>
/// Command to register a new sale transaction. Deducts stock and saves sale atomically.
/// </summary>
/// <param name="PaymentMethod">The payment method selected.</param>
/// <param name="Items">Line items containing product identifiers and quantities.</param>
public sealed record CreateSaleCommand(PaymentMethod PaymentMethod, IReadOnlyList<SaleLineRequest> Items) : IRequest<SaleDto>;

/// <summary>
/// Query to retrieve a paginated history of sales, optionally filtered by date range.
/// Cashiers are restricted to viewing only their own history.
/// </summary>
/// <param name="Page">The page number (1-indexed).</param>
/// <param name="PageSize">Number of items per page.</param>
/// <param name="From">Optional starting date/time bound.</param>
/// <param name="To">Optional ending date/time bound.</param>
public sealed record GetSalesQuery(int Page = 1, int PageSize = 25, DateTime? From = null, DateTime? To = null) : IRequest<PagedResult<SaleDto>>;

/// <summary>
/// Query to retrieve details of a specific sale transaction by its unique identifier.
/// </summary>
/// <param name="Id">The unique identifier of the sale.</param>
public sealed record GetSaleByIdQuery(Guid Id) : IRequest<SaleDto>;

/// <summary>
/// Validator governing rules for registering a new sale.
/// </summary>
public sealed class CreateSaleValidator : AbstractValidator<CreateSaleCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateSaleValidator"/> class.
    /// Requires at least one sale item, with valid product IDs and positive quantities.
    /// </summary>
    public CreateSaleValidator()
    {
        RuleFor(command => command.Items)
            .NotEmpty().WithMessage("Sale must contain at least one item.");

        RuleForEach(command => command.Items).ChildRules(itemRules =>
        {
            itemRules.RuleFor(item => item.ProductId)
                .NotEmpty().WithMessage("Product ID is required.");

            itemRules.RuleFor(item => item.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than zero.");
        });
    }
}

/// <summary>
/// Validator governing rules for listing sales history.
/// </summary>
public sealed class GetSalesValidator : AbstractValidator<GetSalesQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetSalesValidator"/> class.
    /// Ensures valid pagination bounds and correct order of date filters.
    /// </summary>
    public GetSalesValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThan(0).WithMessage("Page must be greater than zero.");

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100.");

        RuleFor(query => query.To)
            .GreaterThanOrEqualTo(query => query.From!.Value)
            .When(query => query.From.HasValue && query.To.HasValue)
            .WithMessage("End date must be greater than or equal to start date.");
    }
}

/// <summary>
/// Consolidated MediatR handlers for Sale commands and queries.
/// </summary>
public sealed class SaleHandlers :
    IRequestHandler<CreateSaleCommand, SaleDto>,
    IRequestHandler<GetSalesQuery, PagedResult<SaleDto>>,
    IRequestHandler<GetSaleByIdQuery, SaleDto>
{
    private readonly IAppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SaleHandlers"/> class.
    /// </summary>
    /// <param name="dbContext">The application database context.</param>
    /// <param name="currentUserService">The current authenticated user service.</param>
    public SaleHandlers(IAppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Maps a domain Sale entity to a SaleDto.
    /// </summary>
    private static SaleDto MapToDto(Sale sale)
    {
        var itemDtos = sale.Items
            .Select(item => new SaleItemDto(
                item.ProductId,
                item.Product?.Name ?? string.Empty,
                item.Quantity,
                item.UnitPrice,
                item.Subtotal))
            .ToList();

        return new SaleDto(
            sale.Id,
            sale.CashierId,
            sale.Cashier?.Email ?? string.Empty,
            sale.TotalAmount,
            sale.PaymentMethod.ToString(),
            sale.CreatedAt,
            itemDtos);
    }

    /// <summary>
    /// Handles creation of a sale. Executes atomically inside a transaction boundary.
    /// Deducts stock, creates Sale and SaleItem records, and calculates totals.
    /// </summary>
    public async Task<SaleDto> Handle(CreateSaleCommand request, CancellationToken cancellationToken)
    {
        // Execute atomically to ensure stock and sales records are updated consistently.
        return await _dbContext.ExecuteInTransactionAsync(async transactionToken =>
        {
            // Group identical product lines by ProductId and sum quantities to optimize DB hits and prevent issues.
            var groupedItems = request.Items
                .GroupBy(item => item.ProductId)
                .Select(group => new SaleLineRequest(group.Key, group.Sum(item => item.Quantity)))
                .ToList();

            var productIds = groupedItems.Select(item => item.ProductId).ToList();

            // Load products referenced in the sale into memory.
            var productsDictionary = await _dbContext.Products
                .Where(product => productIds.Contains(product.Id))
                .ToDictionaryAsync(product => product.Id, transactionToken);

            if (productsDictionary.Count != productIds.Count)
            {
                throw new BadRequestException("One or more products do not exist.");
            }

            // Ensure sufficient stock is available for each line item.
            foreach (var lineItem in groupedItems)
            {
                var product = productsDictionary[lineItem.ProductId];
                if (product.StockQty < lineItem.Quantity)
                {
                    throw new BadRequestException($"Insufficient stock for '{product.Name}'. Only {product.StockQty} units left.");
                }
            }

            // Create new transaction sale
            var sale = new Sale
            {
                CashierId = _currentUserService.UserId,
                PaymentMethod = request.PaymentMethod,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var lineItem in groupedItems)
            {
                var product = productsDictionary[lineItem.ProductId];
                
                // Deduct inventory stock
                product.StockQty -= lineItem.Quantity;

                var saleItem = new SaleItem
                {
                    ProductId = product.Id,
                    Product = product,
                    Quantity = lineItem.Quantity,
                    UnitPrice = product.Price, // Snapshot price to guard against future catalog modifications.
                    Subtotal = product.Price * lineItem.Quantity
                };

                sale.Items.Add(saleItem);
            }

            // Calculate overall sum
            sale.TotalAmount = sale.Items.Sum(item => item.Subtotal);

            _dbContext.Sales.Add(sale);
            await _dbContext.SaveChangesAsync(transactionToken);

            // Populate cashier navigation properties for mapped response.
            sale.Cashier = await _dbContext.Users
                .FindAsync(new object[] { sale.CashierId }, transactionToken);

            return MapToDto(sale);
        }, cancellationToken);
    }

    /// <summary>
    /// Handles listing sales history. Enforces authorization checks and paginates history.
    /// Cashiers can only view their own sales; Owners can view all sales.
    /// </summary>
    public async Task<PagedResult<SaleDto>> Handle(GetSalesQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Sales
            .AsNoTracking()
            .Include(sale => sale.Cashier)
            .Include(sale => sale.Items)
                .ThenInclude(item => item.Product)
            .AsQueryable();

        // Enforce authorization constraints: Cashier role is restricted to their own sales.
        if (!_currentUserService.IsOwner)
        {
            query = query.Where(sale => sale.CashierId == _currentUserService.UserId);
        }

        // Apply date range filters normalized to universal time.
        if (request.From.HasValue)
        {
            query = query.Where(sale => sale.CreatedAt >= request.From.Value.ToUniversalTime());
        }

        if (request.To.HasValue)
        {
            query = query.Where(sale => sale.CreatedAt <= request.To.Value.ToUniversalTime());
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var salesList = await query
            .OrderByDescending(sale => sale.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var saleDtos = salesList.Select(MapToDto).ToList();
        return new PagedResult<SaleDto>(saleDtos, request.Page, request.PageSize, totalCount);
    }

    /// <summary>
    /// Handles retrieving details of a single sale. Enforces cashier access restrictions.
    /// </summary>
    public async Task<SaleDto> Handle(GetSaleByIdQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Sales
            .AsNoTracking()
            .Include(sale => sale.Cashier)
            .Include(sale => sale.Items)
                .ThenInclude(item => item.Product)
            .Where(sale => sale.Id == request.Id);

        // Cashiers cannot read other sales records.
        if (!_currentUserService.IsOwner)
        {
            query = query.Where(sale => sale.CashierId == _currentUserService.UserId);
        }

        var sale = await query.SingleOrDefaultAsync(cancellationToken);

        if (sale is null)
        {
            throw new NotFoundException("Sale", request.Id);
        }

        return MapToDto(sale);
    }
}
