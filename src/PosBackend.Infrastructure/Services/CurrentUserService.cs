using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PosBackend.Application.Common.Interfaces;

namespace PosBackend.Infrastructure.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal User => httpContextAccessor.HttpContext?.User
        ?? throw new UnauthorizedAccessException("An authenticated user is required.");

    public Guid UserId
    {
        get
        {
            // Jwt tokens may be mapped to different claim types by the JWT handler.
            // Try the registered JWT "sub" claim first, then fall back to the mapped NameIdentifier.
            var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                      ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (Guid.TryParse(sub, out var id))
                return id;

            throw new UnauthorizedAccessException("The access token does not contain a valid user id.");
        }
    }

    public bool IsOwner => User.IsInRole("Owner");
}
