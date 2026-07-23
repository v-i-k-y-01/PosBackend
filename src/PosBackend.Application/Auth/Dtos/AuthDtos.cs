namespace PosBackend.Application.Auth.Dtos;

public sealed record UserResponse(Guid Id, string Email, string Role, DateTime CreatedAt);

public sealed record TokenResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);
