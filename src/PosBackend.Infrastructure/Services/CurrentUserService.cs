using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PosBackend.Application.Common.Interfaces;

namespace PosBackend.Infrastructure.Services;

/// <summary>
/// Retrieves properties of the currently authenticated user based on HTTP request security claims.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="CurrentUserService"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">The accessor for current HTTP Context context.</param>
    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Gets the claims principal user context associated with the current HTTP request.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">Thrown if HTTP Context or Claims Principal is not available.</exception>
    private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User
        ?? throw new UnauthorizedAccessException("An authenticated user is required.");

    /// <summary>
    /// Gets the unique identifier (GUID) of the authenticated user.
    /// Tries Sub claim and falls back to NameIdentifier due to claim mapping variations.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">Thrown if access token does not hold a valid user identifier.</exception>
    public Guid UserId
    {
        get
        {
            // JWT tokens may map properties differently based on the handler setup.
            // Try the registered JWT "sub" claim first, then fall back to the mapped NameIdentifier.
            var userClaimId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                              ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (Guid.TryParse(userClaimId, out var parsedUserId))
            {
                return parsedUserId;
            }

            throw new UnauthorizedAccessException("The access token does not contain a valid user id.");
        }
    }

    /// <summary>
    /// Gets the unique identifier (GUID) of the store associated with the authenticated user.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">Thrown if access token does not hold a valid store identifier.</exception>
    public Guid StoreId
    {
        get
        {
            var storeClaimId = User.FindFirstValue("store_id")
                              ?? User.FindFirstValue(ClaimTypes.GroupSid);

            if (Guid.TryParse(storeClaimId, out var parsedStoreId))
            {
                return parsedStoreId;
            }

            throw new UnauthorizedAccessException("The access token does not contain a valid store id.");
        }
    }

    /// <summary>
    /// Gets a value indicating whether the current user has the Owner role.
    /// </summary>
    public bool IsOwner => User.IsInRole("Owner");
}
