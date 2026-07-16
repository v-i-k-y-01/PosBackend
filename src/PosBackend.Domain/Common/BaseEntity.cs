namespace PosBackend.Domain.Common;

/// <summary>
/// Base class for all domain entities. Provides a unique identifier.
/// Domain entities inherit this; nothing in Domain depends on other layers.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
}
