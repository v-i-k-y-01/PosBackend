namespace PosBackend.Domain.Enums;

/// <summary>
/// Roles in the single-shop POS. The first registered user becomes Owner;
/// all subsequent accounts are Cashiers created by the Owner.
/// Stored as a string in the database (see UserConfiguration).
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Represents the shop owner who has full administrative control,
    /// including management of categories, products, team members, and reports.
    /// </summary>
    Owner,

    /// <summary>
    /// Represents a cashier who handles transactions and checkouts.
    /// Cashiers can create and view only their own sales history.
    /// </summary>
    Cashier
}
