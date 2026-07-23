using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PosBackend.Application.Common.Interfaces;

namespace PosBackend.Infrastructure.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal User => httpContextAccessor.HttpContext?.User
        ?? throw new UnauthorizedAccessException("An authenticated user is required.");

    public Guid UserId => Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id)
        ? id : throw new UnauthorizedAccessException("The access token does not contain a valid user id.");

    public bool IsOwner => User.IsInRole("Owner");
}
