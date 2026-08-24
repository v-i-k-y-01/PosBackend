using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PosBackend.Application.Common.Exceptions;

namespace PosBackend.Api.Middleware;

/// <summary>
/// Global exception handling middleware that catches all unhandled exceptions in the request pipeline.
/// Returns consistent JSON error response schemas based on exception type.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private const string JsonContentType = "application/json";
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionHandlingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next delegate in the request pipeline.</param>
    /// <param name="logger">The system logger.</param>
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to intercept exceptions.
    /// </summary>
    /// <param name="context">The current HTTP context context.</param>
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            // Log exceptions as warnings since they represent client or expected flow disruptions.
            // Critical framework failures will propagate and be captured at higher logs level.
            _logger.LogWarning(
                exception,
                "Request execution failed. Method: {Method}, Path: {Path}",
                context.Request.Method,
                context.Request.Path);

            var (statusCode, errorMessage, validationErrors) = MapExceptionToErrorDetails(exception);

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = JsonContentType;

            await context.Response.WriteAsJsonAsync(new
            {
                error = errorMessage,
                statusCode = statusCode,
                errors = validationErrors
            });
        }
    }

    /// <summary>
    /// Maps exception type to corresponding HTTP status code and response properties.
    /// </summary>
    private static (int StatusCode, string Message, IDictionary<string, string[]>? Errors) MapExceptionToErrorDetails(Exception exception)
    {
        return exception switch
        {
            ValidationException validationEx => (
                StatusCodes.Status400BadRequest,
                validationEx.Message,
                validationEx.Errors),

            NotFoundException notFoundEx => (
                StatusCodes.Status404NotFound,
                notFoundEx.Message,
                null),

            ConflictException conflictEx => (
                StatusCodes.Status409Conflict,
                conflictEx.Message,
                null),

            BadRequestException badRequestEx => (
                StatusCodes.Status400BadRequest,
                badRequestEx.Message,
                null),

            ForbiddenException forbiddenEx => (
                StatusCodes.Status403Forbidden,
                forbiddenEx.Message,
                null),

            UnauthorizedAccessException unauthorizedEx => (
                StatusCodes.Status401Unauthorized,
                unauthorizedEx.Message,
                null),

            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                null)
        };
    }
}
