using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PosBackend.Application.Auth.Dtos;
using PosBackend.Application.Common.Interfaces;
using PosBackend.Application.Common.Exceptions;
using PosBackend.Domain.Entities;
using PosBackend.Domain.Enums;

namespace PosBackend.Application.Auth.Commands;

public sealed record RegisterOwnerCommand(string Email, string Password) : IRequest<UserResponse>;

public sealed class RegisterOwnerCommandValidator : AbstractValidator<RegisterOwnerCommand>
{
    public RegisterOwnerCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
    }
}

public sealed class RegisterOwnerCommandHandler(
    IAppDbContext dbContext,
    IPasswordHasher passwordHasher) : IRequestHandler<RegisterOwnerCommand, UserResponse>
{
    public async Task<UserResponse> Handle(RegisterOwnerCommand request, CancellationToken cancellationToken)
    {
        // var ownerExists = await dbContext.Users.AnyAsync(u => u.Role == UserRole.Owner, cancellationToken);
        // if (ownerExists)
        //     throw new ConflictException("The owner account has already been registered.");

        var email = request.Email.Trim().ToLowerInvariant();
        if (await dbContext.Users.AnyAsync(u => u.Email == email, cancellationToken))
            throw new ConflictException("An account with this email already exists.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHasher.Hash(request.Password),
            Role = UserRole.Owner,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new UserResponse(user.Id, user.Email, user.Role.ToString(), user.CreatedAt);
    }
}
