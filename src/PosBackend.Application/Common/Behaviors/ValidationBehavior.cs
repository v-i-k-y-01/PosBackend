using FluentValidation;
using MediatR;
// FluentValidation also defines a ValidationException; alias so this resolves to ours.
using ValidationException = PosBackend.Application.Common.Exceptions.ValidationException;

namespace PosBackend.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that runs all FluentValidation validators registered
/// for the request before the handler executes. Throws ValidationException on failure.
/// Registered as an open behavior in AddApplication().
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var results = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));
            var failures = results
                .SelectMany(r => r.Errors)
                .Where(f => f is not null)
                .ToList();

            if (failures.Count != 0)
            {
                throw new ValidationException(failures);
            }
        }

        return await next();
    }
}
