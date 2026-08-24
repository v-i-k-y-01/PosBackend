namespace PosBackend.Domain.Common;

/// <summary>
/// Base class for all domain entities. Provides a unique identifier.
/// Domain entities inherit this; nothing in Domain depends on other layers.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for this entity.
    /// Defaults to a new GUID value upon initialization.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
}
