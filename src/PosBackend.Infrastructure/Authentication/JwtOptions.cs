namespace PosBackend.Infrastructure.Authentication;

/// <summary>
/// Configuration options for generating and validating JSON Web Tokens (JWT).
/// </summary>
public sealed class JwtOptions
{
    /// <summary>
    /// The configuration section name within application settings (appsettings.json).
    /// </summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// Gets the token issuer identifier (who created the token).
    /// </summary>
    public string Issuer { get; init; } = null!;

    /// <summary>
    /// Gets the token audience identifier (who is the token intended for).
    /// </summary>
    public string Audience { get; init; } = null!;

    /// <summary>
    /// Gets the symmetric security key used to sign and verify the tokens.
    /// Must be at least 32 characters (256 bits) for HMAC-SHA256 signing.
    /// </summary>
    public string Key { get; init; } = null!;

    /// <summary>
    /// Gets the lifetime of the access token in minutes. Defaults to 60.
    /// </summary>
    public int ExpiryMinutes { get; init; } = 60;

    /// <summary>
    /// Gets the lifetime of the refresh token in days. Defaults to 7.
    /// </summary>
    public int RefreshTokenExpiryDays { get; init; } = 7;
}
