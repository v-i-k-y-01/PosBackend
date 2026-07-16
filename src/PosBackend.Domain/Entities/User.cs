using PosBackend.Domain.Common;
using PosBackend.Domain.Enums;

namespace PosBackend.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public UserRole Role { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation: sales this user (cashier) has rung up.
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
