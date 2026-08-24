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
/// <typeparam name="TRequest">Type of the request executing in the pipeline.</typeparam>
/// <typeparam name="TResponse">Type of the response returned by the pipeline.</typeparam>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="validators">The collection of validators registered for the request type.</param>
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    /// <summary>
    /// Intercepts requests in the pipeline, executes all registered validators asynchronously, 
    /// and throws a <see cref="ValidationException"/> if any validation failures are found.
    /// </summary>
    /// <param name="request">The incoming request object.</param>
    /// <param name="next">The delegate to invoke the next behavior or handler in the pipeline.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The response from the handler or subsequent pipeline behaviors.</returns>
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
