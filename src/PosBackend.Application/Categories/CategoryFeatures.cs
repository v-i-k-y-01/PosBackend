using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PosBackend.Application.Common.Exceptions;
using PosBackend.Application.Common.Interfaces;
using PosBackend.Domain.Entities;

namespace PosBackend.Application.Categories;

public sealed record CategoryDto(Guid Id, string Name);
public sealed record CreateCategoryCommand(string Name) : IRequest<CategoryDto>;
public sealed record UpdateCategoryCommand(Guid Id, string Name) : IRequest<CategoryDto>;
public sealed record DeleteCategoryCommand(Guid Id) : IRequest;
public sealed record GetCategoriesQuery : IRequest<IReadOnlyList<CategoryDto>>;
public sealed record GetCategoryByIdQuery(Guid Id) : IRequest<CategoryDto>;

public sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{ public CreateCategoryValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(200); }
public sealed class UpdateCategoryValidator : AbstractValidator<UpdateCategoryCommand>
{ public UpdateCategoryValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Name).NotEmpty().MaximumLength(200); } }

public sealed class CategoryHandlers(IAppDbContext db) :
    IRequestHandler<CreateCategoryCommand, CategoryDto>, IRequestHandler<UpdateCategoryCommand, CategoryDto>,
    IRequestHandler<DeleteCategoryCommand>, IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>,
    IRequestHandler<GetCategoryByIdQuery, CategoryDto>
{
    public async Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        if (await db.Categories.AnyAsync(x => x.Name == name, ct)) throw new ConflictException("A category with this name already exists.");
        var category = new Category { Name = name }; db.Categories.Add(category); await db.SaveChangesAsync(ct); return new(category.Id, category.Name);
    }
    public async Task<CategoryDto> Handle(UpdateCategoryCommand request, CancellationToken ct)
    {
        var category = await db.Categories.FindAsync([request.Id], ct) ?? throw new NotFoundException("Category", request.Id);
        var name = request.Name.Trim();
        if (await db.Categories.AnyAsync(x => x.Id != request.Id && x.Name == name, ct)) throw new ConflictException("A category with this name already exists.");
        category.Name = name; await db.SaveChangesAsync(ct); return new(category.Id, category.Name);
    }
    public async Task Handle(DeleteCategoryCommand request, CancellationToken ct)
    { var category = await db.Categories.FindAsync([request.Id], ct) ?? throw new NotFoundException("Category", request.Id); db.Categories.Remove(category); await db.SaveChangesAsync(ct); }
    public async Task<IReadOnlyList<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken ct) => await db.Categories.AsNoTracking().OrderBy(x => x.Name).Select(x => new CategoryDto(x.Id, x.Name)).ToListAsync(ct);
    public async Task<CategoryDto> Handle(GetCategoryByIdQuery request, CancellationToken ct) => await db.Categories.AsNoTracking().Where(x => x.Id == request.Id).Select(x => new CategoryDto(x.Id, x.Name)).SingleOrDefaultAsync(ct) ?? throw new NotFoundException("Category", request.Id);
}
