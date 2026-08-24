using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PosBackend.Application.Common.Exceptions;
using PosBackend.Application.Common.Interfaces;
using PosBackend.Domain.Entities;

namespace PosBackend.Application.Categories;

/// <summary>
/// Data transfer object representing a category.
/// </summary>
/// <param name="Id">The unique identifier of the category.</param>
/// <param name="Name">The name of the category.</param>
public sealed record CategoryDto(Guid Id, string Name);

/// <summary>
/// Command to create a new category in the store catalog.
/// </summary>
/// <param name="Name">The unique name for the new category.</param>
public sealed record CreateCategoryCommand(string Name) : IRequest<CategoryDto>;

/// <summary>
/// Command to update the details of an existing category.
/// </summary>
/// <param name="Id">The unique identifier of the category to update.</param>
/// <param name="Name">The new name for the category.</param>
public sealed record UpdateCategoryCommand(Guid Id, string Name) : IRequest<CategoryDto>;

/// <summary>
/// Command to delete a category by its identifier.
/// </summary>
/// <param name="Id">The unique identifier of the category to delete.</param>
public sealed record DeleteCategoryCommand(Guid Id) : IRequest;

/// <summary>
/// Query to retrieve a read-only list of all categories, sorted by name.
/// </summary>
public sealed record GetCategoriesQuery : IRequest<IReadOnlyList<CategoryDto>>;

/// <summary>
/// Query to retrieve a specific category's details by its identifier.
/// </summary>
/// <param name="Id">The unique identifier of the category to retrieve.</param>
public sealed record GetCategoryByIdQuery(Guid Id) : IRequest<CategoryDto>;

/// <summary>
/// Validator governing rules for creating a category.
/// </summary>
public sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateCategoryValidator"/> class.
    /// Requires category name to be populated and within character limits.
    /// </summary>
    public CreateCategoryValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(200).WithMessage("Category name cannot exceed 200 characters.");
    }
}

/// <summary>
/// Validator governing rules for updating a category.
/// </summary>
public sealed class UpdateCategoryValidator : AbstractValidator<UpdateCategoryCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateCategoryValidator"/> class.
    /// Requires category ID to be populated, and new name to be valid.
    /// </summary>
    public UpdateCategoryValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty().WithMessage("Category ID is required.");

        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(200).WithMessage("Category name cannot exceed 200 characters.");
    }
}

/// <summary>
/// Consolidated MediatR handlers for Category commands and queries.
/// </summary>
public sealed class CategoryHandlers :
    IRequestHandler<CreateCategoryCommand, CategoryDto>,
    IRequestHandler<UpdateCategoryCommand, CategoryDto>,
    IRequestHandler<DeleteCategoryCommand>,
    IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>,
    IRequestHandler<GetCategoryByIdQuery, CategoryDto>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryHandlers"/> class.
    /// </summary>
    /// <param name="dbContext">The application database context.</param>
    public CategoryHandlers(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Handles the creation of a new category. Ensures uniqueness of name.
    /// </summary>
    public async Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var trimmedName = request.Name.Trim();

        var nameExists = await _dbContext.Categories
            .AnyAsync(category => category.Name == trimmedName, cancellationToken);

        if (nameExists)
        {
            throw new ConflictException("A category with this name already exists.");
        }

        var category = new Category
        {
            Name = trimmedName
        };

        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CategoryDto(category.Id, category.Name);
    }

    /// <summary>
    /// Handles updating an existing category. Checks for existence and duplicate naming.
    /// </summary>
    public async Task<CategoryDto> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _dbContext.Categories
            .FindAsync(new object[] { request.Id }, cancellationToken);

        if (category is null)
        {
            throw new NotFoundException("Category", request.Id);
        }

        var trimmedName = request.Name.Trim();

        var nameConflict = await _dbContext.Categories
            .AnyAsync(cat => cat.Id != request.Id && cat.Name == trimmedName, cancellationToken);

        if (nameConflict)
        {
            throw new ConflictException("A category with this name already exists.");
        }

        category.Name = trimmedName;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CategoryDto(category.Id, category.Name);
    }

    /// <summary>
    /// Handles the deletion of an existing category.
    /// </summary>
    public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _dbContext.Categories
            .FindAsync(new object[] { request.Id }, cancellationToken);

        if (category is null)
        {
            throw new NotFoundException("Category", request.Id);
        }

        _dbContext.Categories.Remove(category);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Handles retrieving all categories sorted alphabetically.
    /// </summary>
    public async Task<IReadOnlyList<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        return await _dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => new CategoryDto(category.Id, category.Name))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Handles retrieving a category by its unique identifier.
    /// </summary>
    public async Task<CategoryDto> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var categoryDto = await _dbContext.Categories
            .AsNoTracking()
            .Where(category => category.Id == request.Id)
            .Select(category => new CategoryDto(category.Id, category.Name))
            .SingleOrDefaultAsync(cancellationToken);

        if (categoryDto is null)
        {
            throw new NotFoundException("Category", request.Id);
        }

        return categoryDto;
    }
}
