namespace PosBackend.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when a user request fails validation or contains invalid parameters.
/// </summary>
/// <param name="message">The message describing the validation failure or bad request reason.</param>
public sealed class BadRequestException(string message) : Exception(message);
