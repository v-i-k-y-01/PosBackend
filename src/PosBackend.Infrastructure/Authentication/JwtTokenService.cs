using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PosBackend.Application.Auth.Dtos;
using PosBackend.Application.Common.Interfaces;
using PosBackend.Domain.Entities;

namespace PosBackend.Infrastructure.Authentication;

/// <summary>
/// Service implementation for generating security tokens (JWT) for authenticated users.
/// </summary>
public sealed class JwtTokenService : ITokenService
{
    private const string TokenTypeClaim = "token_type";
    private const string AccessTokenType = "access";
    private const string RefreshTokenType = "refresh";

    private readonly JwtOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtTokenService"/> class.
    /// </summary>
    /// <param name="options">The configured JWT options instance.</param>
    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// Generates access and refresh JSON Web Tokens (JWT) for the authenticated user.
    /// </summary>
    /// <param name="user">The user entity context requesting tokens.</param>
    /// <returns>A structured TokenResponse containing both tokens and their expiration boundaries.</returns>
    public TokenResponse CreateTokens(User user)
    {
        var currentUtcTime = DateTime.UtcNow;
        var accessTokenExpiresAt = currentUtcTime.AddMinutes(_options.ExpiryMinutes);
        var refreshTokenExpiresAt = currentUtcTime.AddDays(_options.RefreshTokenExpiryDays);

        var accessToken = CreateToken(user, accessTokenExpiresAt, AccessTokenType);
        var refreshToken = CreateToken(user, refreshTokenExpiresAt, RefreshTokenType);

        return new TokenResponse(
            accessToken,
            accessTokenExpiresAt,
            refreshToken,
            refreshTokenExpiresAt);
    }

    /// <summary>
    /// Generates a signed JWT with specific claims and expiration boundaries.
    /// </summary>
    private string CreateToken(User user, DateTime expiresAt, string tokenType)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(TokenTypeClaim, tokenType)
        };

        var securityToken = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(securityToken);
    }
}
