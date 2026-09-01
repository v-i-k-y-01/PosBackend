namespace PosBackend.Application.Common.Interfaces;

/// <summary>
/// Abstraction to access properties of the currently authenticated user.
/// Decouples the Application layer from HTTP Context / ASP.NET structures.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the unique identifier of the currently authenticated user.
    /// Throws an exception if user is not authenticated.
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// Gets the unique identifier of the store the authenticated user belongs to.
    /// Throws an exception if user is not authenticated or token lacks store claim.
    /// </summary>
    Guid StoreId { get; }

    /// <summary>
    /// Gets a value indicating whether the current user has the Owner role.
    /// </summary>
    bool IsOwner { get; }
}
