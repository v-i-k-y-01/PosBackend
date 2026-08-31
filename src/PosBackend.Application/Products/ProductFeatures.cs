using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PosBackend.Application.Common.Exceptions;
using PosBackend.Application.Common.Interfaces;
using PosBackend.Domain.Entities;

namespace PosBackend.Application.Products;

/// <summary>
/// Data transfer object representing a product.
/// </summary>
/// <param name="Id">Unique product identifier.</param>
/// <param name="CategoryId">Associated category identifier (optional).</param>
/// <param name="CategoryName">Name of the category (optional).</param>
/// <param name="Name">Product display name.</param>
/// <param name="Sku">Unique Stock Keeping Unit.</param>
/// <param name="Price">Product unit price.</param>
/// <param name="StockQty">Current available inventory quantity.</param>
/// <param name="CreatedAt">Timestamp when product was added.</param>
public sealed record ProductDto(
    Guid Id,
    Guid? CategoryId,
    string? CategoryName,
    string Name,
    string Sku,
    decimal Price,
    int StockQty,
    DateTime CreatedAt);

/// <summary>
/// Command to create a new product.
/// </summary>
public sealed record CreateProductCommand(
    Guid? CategoryId,
    string Name,
    string Sku,
    decimal Price,
    int StockQty) : IRequest<ProductDto>;

/// <summary>
/// Command to update properties of an existing product.
/// </summary>
public sealed record UpdateProductCommand(
    Guid Id,
    Guid? CategoryId,
    string Name,
    string Sku,
    decimal Price,
    int StockQty) : IRequest<ProductDto>;

/// <summary>
/// Command to delete a product by its identifier.
/// </summary>
public sealed record DeleteProductCommand(Guid Id) : IRequest;

/// <summary>
/// Query to search and filter products.
/// </summary>
public sealed record GetProductsQuery(Guid? CategoryId, string? Search) : IRequest<IReadOnlyList<ProductDto>>;

/// <summary>
/// Query to retrieve a product by its identifier.
/// </summary>
public sealed record GetProductByIdQuery(Guid Id) : IRequest<ProductDto>;

/// <summary>
/// Query to retrieve a product by its barcode or SKU number.
/// </summary>
/// <param name="Barcode">The barcode or SKU string.</param>
public sealed record GetProductByBarcodeQuery(string Barcode) : IRequest<ProductDto>;

/// <summary>
/// DTO representing an auto-generated unique barcode.
/// </summary>
/// <param name="Barcode">The 12-digit unique barcode string.</param>
public sealed record GeneratedBarcodeDto(string Barcode);

/// <summary>
/// Query to generate a guaranteed unique standard 12-digit barcode.
/// </summary>
public sealed record GenerateBarcodeQuery : IRequest<GeneratedBarcodeDto>;


/// <summary>
/// Validator governing rules for creating a product.
/// </summary>
public sealed class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateProductValidator"/> class.
    /// </summary>
    public CreateProductValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(300).WithMessage("Product name cannot exceed 300 characters.");

        RuleFor(command => command.Sku)
            .NotEmpty().WithMessage("SKU is required.")
            .MaximumLength(100).WithMessage("SKU cannot exceed 100 characters.");

        RuleFor(command => command.Price)
            .GreaterThan(0).WithMessage("Price must be greater than zero.");

        RuleFor(command => command.StockQty)
            .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative.");
    }
}

/// <summary>
/// Validator governing rules for updating a product.
/// </summary>
public sealed class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateProductValidator"/> class.
    /// </summary>
    public UpdateProductValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty().WithMessage("Product ID is required.");

        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(300).WithMessage("Product name cannot exceed 300 characters.");

        RuleFor(command => command.Sku)
            .NotEmpty().WithMessage("SKU is required.")
            .MaximumLength(100).WithMessage("SKU cannot exceed 100 characters.");

        RuleFor(command => command.Price)
            .GreaterThan(0).WithMessage("Price must be greater than zero.");

        RuleFor(command => command.StockQty)
            .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative.");
    }
}

/// <summary>
/// Consolidated MediatR handlers for Product commands and queries.
/// </summary>
public sealed class ProductHandlers :
    IRequestHandler<CreateProductCommand, ProductDto>,
    IRequestHandler<UpdateProductCommand, ProductDto>,
    IRequestHandler<DeleteProductCommand>,
    IRequestHandler<GetProductsQuery, IReadOnlyList<ProductDto>>,
    IRequestHandler<GetProductByIdQuery, ProductDto>,
    IRequestHandler<GetProductByBarcodeQuery, ProductDto>,
    IRequestHandler<GenerateBarcodeQuery, GeneratedBarcodeDto>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductHandlers"/> class.
    /// </summary>
    /// <param name="dbContext">The application database context.</param>
    public ProductHandlers(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Maps a domain Product entity to a ProductDto.
    /// </summary>
    private static ProductDto MapToDto(Product product)
    {
        return new ProductDto(
            product.Id,
            product.CategoryId,
            product.Category?.Name,
            product.Name,
            product.Sku,
            product.Price,
            product.StockQty,
            product.CreatedAt);
    }

    /// <summary>
    /// Validates that the referenced category exists in the system.
    /// </summary>
    private async Task EnsureCategoryExistsAsync(Guid? categoryId, CancellationToken cancellationToken)
    {
        if (categoryId.HasValue)
        {
            var categoryExists = await _dbContext.Categories
                .AnyAsync(category => category.Id == categoryId.Value, cancellationToken);

            if (!categoryExists)
            {
                throw new NotFoundException("Category", categoryId.Value);
            }
        }
    }

    /// <summary>
    /// Handles product creation. Checks for SKU conflicts and verifies category.
    /// </summary>
    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        await EnsureCategoryExistsAsync(request.CategoryId, cancellationToken);

        var trimmedSku = request.Sku.Trim();

        var skuExists = await _dbContext.Products
            .AnyAsync(product => product.Sku == trimmedSku, cancellationToken);

        if (skuExists)
        {
            throw new ConflictException("A product with this SKU already exists.");
        }

        var product = new Product
        {
            CategoryId = request.CategoryId,
            Name = request.Name.Trim(),
            Sku = trimmedSku,
            Price = request.Price,
            StockQty = request.StockQty,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Populate category relation for the mapped DTO response.
        if (product.CategoryId.HasValue)
        {
            product.Category = await _dbContext.Categories
                .FindAsync(new object[] { product.CategoryId.Value }, cancellationToken);
        }

        return MapToDto(product);
    }

    /// <summary>
    /// Handles product updates. Validates that product exists, checks for SKU conflicts, and saves details.
    /// </summary>
    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .Include(p => p.Category)
            .SingleOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException("Product", request.Id);
        }

        await EnsureCategoryExistsAsync(request.CategoryId, cancellationToken);

        var trimmedSku = request.Sku.Trim();

        var skuConflict = await _dbContext.Products
            .AnyAsync(p => p.Id != request.Id && p.Sku == trimmedSku, cancellationToken);

        if (skuConflict)
        {
            throw new ConflictException("A product with this SKU already exists.");
        }

        product.CategoryId = request.CategoryId;
        product.Name = request.Name.Trim();
        product.Sku = trimmedSku;
        product.Price = request.Price;
        product.StockQty = request.StockQty;

        // Refresh category navigation property
        product.Category = request.CategoryId.HasValue
            ? await _dbContext.Categories.FindAsync(new object[] { request.CategoryId.Value }, cancellationToken)
            : null;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(product);
    }

    /// <summary>
    /// Handles product deletion. Restricts deletion if product has historical sale items.
    /// </summary>
    public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .FindAsync(new object[] { request.Id }, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException("Product", request.Id);
        }

        // Restrict deletion if referenced by any sale items.
        var hasSaleHistory = await _dbContext.SaleItems
            .AnyAsync(item => item.ProductId == request.Id, cancellationToken);

        if (hasSaleHistory)
        {
            throw new ConflictException("A product with sale history cannot be deleted.");
        }

        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Handles querying and filtering the products catalog.
    /// </summary>
    public async Task<IReadOnlyList<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .AsQueryable();

        if (request.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var trimmedSearch = request.Search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(trimmedSearch) || p.Sku.ToLower().Contains(trimmedSearch));
        }

        var products = await query
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return products.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Handles retrieving a single product by its unique identifier.
    /// </summary>
    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .SingleOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException("Product", request.Id);
        }

        return MapToDto(product);
    }

    /// <summary>
    /// Handles retrieving a single product by its exact barcode or SKU string.
    /// </summary>
    public async Task<ProductDto> Handle(GetProductByBarcodeQuery request, CancellationToken cancellationToken)
    {
        var trimmedBarcode = request.Barcode.Trim();

        var product = await _dbContext.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .SingleOrDefaultAsync(p => p.Sku == trimmedBarcode, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException("Product with Barcode / SKU", request.Barcode);
        }

        return MapToDto(product);
    }

    /// <summary>
    /// Handles generating a guaranteed unique standard 12-digit barcode.
    /// </summary>
    public async Task<GeneratedBarcodeDto> Handle(GenerateBarcodeQuery request, CancellationToken cancellationToken)
    {
        var random = new Random();
        string barcode;
        bool exists;

        do
        {
            var randomDigits = random.NextInt64(100000000L, 999999999L);
            barcode = $"890{randomDigits}";
            exists = await _dbContext.Products.AnyAsync(p => p.Sku == barcode, cancellationToken);
        } while (exists);

        return new GeneratedBarcodeDto(barcode);
    }
}

