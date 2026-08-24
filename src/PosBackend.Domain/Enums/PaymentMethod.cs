namespace PosBackend.Domain.Enums;

/// <summary>
/// How a sale was paid for. Stored as a string in the database (see SaleConfiguration).
/// </summary>
public enum PaymentMethod
{
    /// <summary>
    /// Transaction settled using physical cash currency.
    /// </summary>
    Cash,

    /// <summary>
    /// Transaction settled using credit, debit, or gift card payment methods.
    /// </summary>
    Card,

    /// <summary>
    /// Transaction settled using Unified Payments Interface (UPI) mobile payment.
    /// </summary>
    Upi
}
