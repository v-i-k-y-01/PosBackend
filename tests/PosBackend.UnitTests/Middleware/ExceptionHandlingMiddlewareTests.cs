using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using PosBackend.Api.Middleware;
using PosBackend.Application.Common.Exceptions;
using Xunit;

namespace PosBackend.UnitTests.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    private readonly Mock<ILogger<ExceptionHandlingMiddleware>> _loggerMock;

    public ExceptionHandlingMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<ExceptionHandlingMiddleware>>();
    }

    [Fact]
    public async Task Invoke_ShouldCallNext_WhenNoExceptionIsThrown()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object);

        // Act
        await middleware.Invoke(context);

        // Assert
        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task Invoke_ShouldWriteErrorResponse_WhenValidationExceptionIsThrown()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var responseStream = new MemoryStream();
        context.Response.Body = responseStream;

        var validationErrors = new List<FluentValidation.Results.ValidationFailure>
        {
            new FluentValidation.Results.ValidationFailure("Name", "Name is required.")
        };
        var exception = new ValidationException(validationErrors);

        RequestDelegate next = (ctx) => throw exception;

        var middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object);

        // Act
        await middleware.Invoke(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        context.Response.ContentType.Should().StartWith("application/json");

        responseStream.Position = 0;
        using var reader = new StreamReader(responseStream);
        var json = await reader.ReadToEndAsync();
        var response = JsonSerializer.Deserialize<ErrorResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        response.Should().NotBeNull();
        response!.Error.Should().Contain("One or more validation failures");
        response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        response.Errors.Should().NotBeNull();
        response.Errors!.Should().ContainKey("Name");
    }

    [Fact]
    public async Task Invoke_ShouldWriteErrorResponse_WhenNotFoundExceptionIsThrown()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var responseStream = new MemoryStream();
        context.Response.Body = responseStream;

        var exception = new NotFoundException("Product", "SKU123");
        RequestDelegate next = (ctx) => throw exception;

        var middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object);

        // Act
        await middleware.Invoke(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);

        responseStream.Position = 0;
        using var reader = new StreamReader(responseStream);
        var json = await reader.ReadToEndAsync();
        var response = JsonSerializer.Deserialize<ErrorResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        response.Should().NotBeNull();
        response!.Error.Should().Be(exception.Message);
        response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    private class ErrorResponse
    {
        public string Error { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }
    }
}
