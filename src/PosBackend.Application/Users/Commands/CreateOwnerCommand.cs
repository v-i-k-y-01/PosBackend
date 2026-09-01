using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PosBackend.Application.Auth.Dtos;
using PosBackend.Application.Common.Exceptions;
using PosBackend.Application.Common.Interfaces;
using PosBackend.Domain.Entities;
using PosBackend.Domain.Enums;

namespace PosBackend.Application.Users.Commands;

/// <summary>
/// Command to create a new user account with the Owner role.
/// Must be invoked by an authenticated Owner user.
/// </summary>
/// <param name="Email">The email address for the new Owner account.</param>
/// <param name="Password">The desired plain text password (minimum 8 characters).</param>
public sealed record CreateOwnerCommand(string Email, string Password) : IRequest<UserResponse>;

/// <summary>
/// Validator governing rules for creating a new Owner user.
/// </summary>
public sealed class CreateOwnerCommandValidator : AbstractValidator<CreateOwnerCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateOwnerCommandValidator"/> class.
    /// Defines validation requirements for the new owner's email and password.
    /// </summary>
    public CreateOwnerCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.");

        RuleFor(command => command.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .MaximumLength(128).WithMessage("Password cannot exceed 128 characters.");
    }
}

/// <summary>
/// Handler responsible for creating and persisting a new user with the Owner role.
/// </summary>
public sealed class CreateOwnerCommandHandler : IRequestHandler<CreateOwnerCommand, UserResponse>
{
    private readonly IAppDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserService _currentUserService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateOwnerCommandHandler"/> class.
    /// </summary>
    /// <param name="dbContext">The application database context.</param>
    /// <param name="passwordHasher">The password hashing utility.</param>
    /// <param name="currentUserService">The service providing the authenticated user context.</param>
    public CreateOwnerCommandHandler(
        IAppDbContext dbContext,
        IPasswordHasher passwordHasher,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Checks for duplicate user, hashes password, and persists the new Owner user.
    /// </summary>
    /// <param name="request">The creation command containing new user parameters.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A user response DTO representing the newly created Owner.</returns>
    /// <exception cref="ConflictException">Thrown if a user with the same email already exists.</exception>
    public async Task<UserResponse> Handle(CreateOwnerCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var emailExists = await _dbContext.Users
            .AnyAsync(user => user.Email == normalizedEmail, cancellationToken);

        if (emailExists)
        {
            throw new ConflictException("An account with this email already exists.");
        }

        var newUser = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = UserRole.Owner,
            StoreId = _currentUserService.StoreId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(newUser);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new UserResponse(
            newUser.Id,
            newUser.Email,
            newUser.Role.ToString(),
            newUser.StoreId,
            newUser.CreatedAt);
    }
}
