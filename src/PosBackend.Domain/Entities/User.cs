using PosBackend.Domain.Common;
using PosBackend.Domain.Enums;

namespace PosBackend.Domain.Entities;

/// <summary>
/// Represents a user account in the Point-of-Sale (POS) system.
/// A user can have different roles (Owner, Cashier) governing access and authority.
/// </summary>
public class User : BaseEntity
{
    /// <summary>
    /// Gets or sets the email address of the user. Email is unique across all users.
    /// </summary>
    public string Email { get; set; } = null!;

    /// <summary>
    /// Gets or sets the secure salted BCrypt hash of the user's password.
    /// </summary>
    public string PasswordHash { get; set; } = null!;

    /// <summary>
    /// Gets or sets the access and authority role of the user (e.g., Owner, Cashier).
    /// </summary>
    public UserRole Role { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this user account was created (stored in UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the store this user belongs to.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the store this user belongs to.
    /// </summary>
    public Store? Store { get; set; }

    /// <summary>
    /// Gets or sets the collection of sales rung up by this user.
    /// Primarily used for tracking sales associated with a cashier.
    /// </summary>
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
