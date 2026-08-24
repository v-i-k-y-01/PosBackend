using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PosBackend.Application.Auth.Dtos;
using PosBackend.Application.Common.Interfaces;

namespace PosBackend.Application.Auth.Commands;

/// <summary>
/// Command to request user authentication via email and password credentials.
/// Returns JWT access and refresh tokens upon success.
/// </summary>
/// <param name="Email">The user's registered email address.</param>
/// <param name="Password">The user's plain text password.</param>
public sealed record LoginCommand(string Email, string Password) : IRequest<TokenResponse>;

/// <summary>
/// Validator governing rules for authentication credentials in the LoginCommand.
/// </summary>
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoginCommandValidator"/> class.
    /// Defines validation rules for email presence/format and password presence.
    /// </summary>
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.");

        RuleFor(command => command.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MaximumLength(128).WithMessage("Password cannot exceed 128 characters.");
    }
}

/// <summary>
/// Handler responsible for executing user login logic. Verifies credentials and generates JWTs.
/// </summary>
public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, TokenResponse>
{
    private readonly IAppDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginCommandHandler"/> class.
    /// </summary>
    /// <param name="dbContext">The application database context.</param>
    /// <param name="passwordHasher">The password hashing utility.</param>
    /// <param name="tokenService">The token generation service.</param>
    public LoginCommandHandler(
        IAppDbContext dbContext,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    /// <summary>
    /// Handles authentication requests, validates credentials against the database, and returns JWT tokens.
    /// </summary>
    /// <param name="request">The login request command containing credentials.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A token response containing access and refresh tokens.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown if email is not found or password verification fails.</exception>
    public async Task<TokenResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Normalize email to match case-insensitive storage.
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _dbContext.Users
            .SingleOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            // Fail genericly to avoid exposing account existence details.
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        return _tokenService.CreateTokens(user);
    }
}
