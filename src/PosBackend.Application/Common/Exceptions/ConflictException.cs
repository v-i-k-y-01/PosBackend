namespace PosBackend.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when a request conflicts with the current state of the system (e.g. duplicate unique SKU or category name).
/// </summary>
/// <param name="message">The message describing the conflict.</param>
public sealed class ConflictException(string message) : Exception(message);
