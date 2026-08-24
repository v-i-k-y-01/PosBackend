using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PosBackend.Application.Auth.Dtos;
using PosBackend.Application.Common.Interfaces;
using PosBackend.Application.Common.Exceptions;
using PosBackend.Domain.Entities;
using PosBackend.Domain.Enums;

namespace PosBackend.Application.Auth.Commands;

/// <summary>
/// Command to register the initial Owner account of the POS system.
/// </summary>
/// <param name="Email">The requested email address for the owner account.</param>
/// <param name="Password">The desired plain text password (minimum 8 characters).</param>
public sealed record RegisterOwnerCommand(string Email, string Password) : IRequest<UserResponse>;

/// <summary>
/// Validator governing rules for owner registration.
/// </summary>
public sealed class RegisterOwnerCommandValidator : AbstractValidator<RegisterOwnerCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterOwnerCommandValidator"/> class.
    /// Defines validation requirements for owner email and password complexity.
    /// </summary>
    public RegisterOwnerCommandValidator()
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
/// Handler executing logic to register the initial Owner user in the database.
/// </summary>
public sealed class RegisterOwnerCommandHandler : IRequestHandler<RegisterOwnerCommand, UserResponse>
{
    private readonly IAppDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterOwnerCommandHandler"/> class.
    /// </summary>
    /// <param name="dbContext">The application database context.</param>
    /// <param name="passwordHasher">The password hashing utility.</param>
    public RegisterOwnerCommandHandler(IAppDbContext dbContext, IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    /// <summary>
    /// Processes owner registration, checks for duplicate email, hashes password, and persists the new Owner.
    /// </summary>
    /// <param name="request">The registration command containing credentials.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A user response DTO representing the newly registered owner.</returns>
    /// <exception cref="ConflictException">Thrown if a user with the same email already exists.</exception>
    public async Task<UserResponse> Handle(RegisterOwnerCommand request, CancellationToken cancellationToken)
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
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(newUser);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new UserResponse(
            newUser.Id,
            newUser.Email,
            newUser.Role.ToString(),
            newUser.CreatedAt);
    }
}
