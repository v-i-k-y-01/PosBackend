using PosBackend.Application.Auth.Dtos;
using PosBackend.Domain.Entities;

namespace PosBackend.Application.Common.Interfaces;

/// <summary>
/// Service responsible for generating JWT access and refresh tokens.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates access and refresh tokens for the specified user.
    /// </summary>
    /// <param name="user">The user entity for which tokens are generated.</param>
    /// <returns>A TokenResponse containing the access token, refresh token, and their expirations.</returns>
    TokenResponse CreateTokens(User user);
}
