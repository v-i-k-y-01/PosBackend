using Microsoft.AspNetCore.Mvc;
using PosBackend.Application.Common.Exceptions;

namespace PosBackend.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try { await next(context); }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Request failed: {Method} {Path}", context.Request.Method, context.Request.Path);
            var (status, message, errors) = ex switch
            {
                ValidationException v => (StatusCodes.Status400BadRequest, v.Message, v.Errors),
                NotFoundException n => (StatusCodes.Status404NotFound, n.Message, null),
                ConflictException c => (StatusCodes.Status409Conflict, c.Message, null),
                BadRequestException b => (StatusCodes.Status400BadRequest, b.Message, null),
                ForbiddenException f => (StatusCodes.Status403Forbidden, f.Message, null),
                UnauthorizedAccessException u => (StatusCodes.Status401Unauthorized, u.Message, null),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", null)
            };
            context.Response.StatusCode=status; context.Response.ContentType="application/json";
            await context.Response.WriteAsJsonAsync(new { error=message, statusCode=status, errors });
        }
    }
}
