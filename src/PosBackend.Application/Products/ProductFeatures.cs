using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PosBackend.Application.Common.Exceptions;
using PosBackend.Application.Common.Interfaces;
using PosBackend.Domain.Entities;

namespace PosBackend.Application.Products;

public sealed record ProductDto(Guid Id, Guid? CategoryId, string? CategoryName, string Name, string Sku, decimal Price, int StockQty, DateTime CreatedAt);
public sealed record CreateProductCommand(Guid? CategoryId, string Name, string Sku, decimal Price, int StockQty) : IRequest<ProductDto>;
public sealed record UpdateProductCommand(Guid Id, Guid? CategoryId, string Name, string Sku, decimal Price, int StockQty) : IRequest<ProductDto>;
public sealed record DeleteProductCommand(Guid Id) : IRequest;
public sealed record GetProductsQuery(Guid? CategoryId, string? Search) : IRequest<IReadOnlyList<ProductDto>>;
public sealed record GetProductByIdQuery(Guid Id) : IRequest<ProductDto>;
public sealed class CreateProductValidator : AbstractValidator<CreateProductCommand> { public CreateProductValidator() { Rules(); } void Rules() { RuleFor(x => x.Name).NotEmpty().MaximumLength(300); RuleFor(x => x.Sku).NotEmpty().MaximumLength(100); RuleFor(x => x.Price).GreaterThan(0); RuleFor(x => x.StockQty).GreaterThanOrEqualTo(0); } }
public sealed class UpdateProductValidator : AbstractValidator<UpdateProductCommand> { public UpdateProductValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Name).NotEmpty().MaximumLength(300); RuleFor(x => x.Sku).NotEmpty().MaximumLength(100); RuleFor(x => x.Price).GreaterThan(0); RuleFor(x => x.StockQty).GreaterThanOrEqualTo(0); } }
public sealed class ProductHandlers(IAppDbContext db) : IRequestHandler<CreateProductCommand, ProductDto>, IRequestHandler<UpdateProductCommand, ProductDto>, IRequestHandler<DeleteProductCommand>, IRequestHandler<GetProductsQuery, IReadOnlyList<ProductDto>>, IRequestHandler<GetProductByIdQuery, ProductDto>
{
    static ProductDto Map(Product x) => new(x.Id, x.CategoryId, x.Category?.Name, x.Name, x.Sku, x.Price, x.StockQty, x.CreatedAt);
    async Task ValidateCategory(Guid? id, CancellationToken ct) { if (id.HasValue && !await db.Categories.AnyAsync(x => x.Id == id, ct)) throw new NotFoundException("Category", id.Value); }
    public async Task<ProductDto> Handle(CreateProductCommand r, CancellationToken ct) { await ValidateCategory(r.CategoryId, ct); var sku=r.Sku.Trim(); if(await db.Products.AnyAsync(x=>x.Sku==sku,ct)) throw new ConflictException("A product with this SKU already exists."); var p=new Product { CategoryId=r.CategoryId, Name=r.Name.Trim(), Sku=sku, Price=r.Price, StockQty=r.StockQty, CreatedAt=DateTime.UtcNow}; db.Products.Add(p); await db.SaveChangesAsync(ct); p.Category = r.CategoryId.HasValue ? await db.Categories.FindAsync([r.CategoryId.Value], ct) : null; return Map(p); }
    public async Task<ProductDto> Handle(UpdateProductCommand r, CancellationToken ct) { var p=await db.Products.Include(x=>x.Category).SingleOrDefaultAsync(x=>x.Id==r.Id,ct)??throw new NotFoundException("Product",r.Id); await ValidateCategory(r.CategoryId,ct); var sku=r.Sku.Trim(); if(await db.Products.AnyAsync(x=>x.Id!=r.Id&&x.Sku==sku,ct))throw new ConflictException("A product with this SKU already exists."); p.CategoryId=r.CategoryId;p.Name=r.Name.Trim();p.Sku=sku;p.Price=r.Price;p.StockQty=r.StockQty;p.Category=r.CategoryId.HasValue?await db.Categories.FindAsync([r.CategoryId.Value],ct):null;await db.SaveChangesAsync(ct);return Map(p); }
    public async Task Handle(DeleteProductCommand r, CancellationToken ct) { var p=await db.Products.FindAsync([r.Id],ct)??throw new NotFoundException("Product",r.Id); if(await db.SaleItems.AnyAsync(x=>x.ProductId==r.Id,ct))throw new ConflictException("A product with sale history cannot be deleted."); db.Products.Remove(p);await db.SaveChangesAsync(ct); }
    public async Task<IReadOnlyList<ProductDto>> Handle(GetProductsQuery r,CancellationToken ct) { var q=db.Products.AsNoTracking().Include(x=>x.Category).AsQueryable();if(r.CategoryId.HasValue)q=q.Where(x=>x.CategoryId==r.CategoryId);if(!string.IsNullOrWhiteSpace(r.Search)){var s=r.Search.Trim();q=q.Where(x=>x.Name.Contains(s)||x.Sku.Contains(s));}return (await q.OrderBy(x=>x.Name).ToListAsync(ct)).Select(Map).ToList(); }
    public async Task<ProductDto> Handle(GetProductByIdQuery r,CancellationToken ct) {var p=await db.Products.AsNoTracking().Include(x=>x.Category).SingleOrDefaultAsync(x=>x.Id==r.Id,ct)??throw new NotFoundException("Product",r.Id);return Map(p);}
}
