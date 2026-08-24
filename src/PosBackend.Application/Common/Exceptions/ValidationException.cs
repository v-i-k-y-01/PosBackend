using FluentValidation.Results;

namespace PosBackend.Application.Common.Exceptions;

/// <summary>
/// Exception thrown by the validation pipeline behavior when one or more registered validators fail.
/// Carries a structured dictionary mapping invalid properties to their validation error messages.
/// </summary>
public class ValidationException : Exception
{
    /// <summary>
    /// Gets the structured dictionary of validation errors.
    /// Key represents the property name, and Value represents the array of validation failure messages.
    /// </summary>
    public IDictionary<string, string[]> Errors { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class with a generic validation error message.
    /// </summary>
    public ValidationException()
        : base("One or more validation failures have occurred.")
    {
        Errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class based on a collection of validation failures.
    /// Groups errors by property name.
    /// </summary>
    /// <param name="failures">The collection of FluentValidation validation failures.</param>
    public ValidationException(IEnumerable<ValidationFailure> failures) : this()
    {
        Errors = failures
            .GroupBy(f => f.PropertyName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).Distinct().ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }
}
