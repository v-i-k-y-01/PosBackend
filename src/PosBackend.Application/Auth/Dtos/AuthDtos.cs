namespace PosBackend.Application.Auth.Dtos;

/// <summary>
/// Data transfer object representing the user details returned upon successful creation or retrieval.
/// </summary>
/// <param name="Id">The unique identifier of the user.</param>
/// <param name="Email">The email address of the user.</param>
/// <param name="Role">The role assigned to the user (e.g. Owner, Cashier).</param>
/// <param name="StoreId">The unique identifier of the store the user belongs to.</param>
/// <param name="CreatedAt">The date and time when the user was created.</param>
public sealed record UserResponse(Guid Id, string Email, string Role, Guid StoreId, DateTime CreatedAt);

/// <summary>
/// Data transfer object carrying access and refresh tokens returned after authentication.
/// </summary>
/// <param name="AccessToken">The JWT access token used to authenticate subsequent API requests.</param>
/// <param name="AccessTokenExpiresAt">The expiration timestamp for the access token in UTC.</param>
/// <param name="RefreshToken">The JWT refresh token used to request new access tokens.</param>
/// <param name="RefreshTokenExpiresAt">The expiration timestamp for the refresh token in UTC.</param>
public sealed record TokenResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);
