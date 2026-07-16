using FluentValidation.Results;

namespace PosBackend.Application.Common.Exceptions;

/// <summary>
/// Thrown by the validation pipeline behavior when one or more validators fail.
/// Carries a dictionary of property -> error messages for a structured response.
/// </summary>
public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException()
        : base("One or more validation failures have occurred.")
    {
        Errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
    }

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
