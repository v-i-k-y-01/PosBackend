namespace PosBackend.Domain.Enums;

/// <summary>
/// Roles in the single-shop POS. The first registered user becomes Owner;
/// all subsequent accounts are Cashiers created by the Owner.
/// Stored as a string in the database (see UserConfiguration).
/// </summary>
public enum UserRole
{
    Owner,
    Cashier
}
