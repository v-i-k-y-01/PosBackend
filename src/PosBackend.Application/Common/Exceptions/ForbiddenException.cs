namespace PosBackend.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when an authenticated user attempts to access a resource or perform an action 
/// that is outside their assigned roles or permissions.
/// </summary>
public class ForbiddenException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ForbiddenException"/> class with a default or custom error message.
    /// </summary>
    /// <param name="message">The message explaining why the action was forbidden.</param>
    public ForbiddenException(string message = "You do not have permission to perform this action.")
        : base(message) { }
}
