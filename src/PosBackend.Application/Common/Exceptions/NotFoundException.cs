namespace PosBackend.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when a requested entity cannot be found in the system.
/// </summary>
public class NotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message describing the error.</param>
    public NotFoundException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class for a specific entity name and key identifier.
    /// </summary>
    /// <param name="name">The name of the entity that was not found (e.g. Product, Category).</param>
    /// <param name="key">The key identifier used to lookup the entity.</param>
    public NotFoundException(string name, object key)
        : base($"Entity \"{name}\" ({key}) was not found.") { }
}
