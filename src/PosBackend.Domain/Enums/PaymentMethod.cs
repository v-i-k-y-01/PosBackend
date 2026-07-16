namespace PosBackend.Domain.Enums;

/// <summary>
/// How a sale was paid for. Stored as a string in the database (see SaleConfiguration).
/// </summary>
public enum PaymentMethod
{
    Cash,
    Card,
    Upi
}
