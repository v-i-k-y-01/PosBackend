using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PosBackend.Application.Auth.Commands;
using PosBackend.Application.Auth.Dtos;

namespace PosBackend.Api.Controllers;

/// <summary>
/// Handles authentication endpoints such as owner registration and login.
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/> class.
    /// </summary>
    /// <param name="sender">The MediatR sender utility.</param>
    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Registers the initial Owner account for the Point-of-Sale shop.
    /// </summary>
    /// <param name="request">Payload carrying the email and password for the owner.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The details of the created owner account.</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var registeredUser = await _sender.Send(
            new RegisterOwnerCommand(request.Email, request.Password, request.StoreName),
            cancellationToken);

        return Created(string.Empty, registeredUser);
    }

    /// <summary>
    /// Authenticates a user with email and password, returning access and refresh JWTs.
    /// </summary>
    /// <param name="request">Payload carrying login credentials.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>Tokens and expiration information.</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var tokenResponse = await _sender.Send(
            new LoginCommand(request.Email, request.Password),
            cancellationToken);

        return Ok(tokenResponse);
    }
}

/// <summary>
/// Request contract for Owner registration.
/// </summary>
/// <param name="Email">The requested email address.</param>
/// <param name="Password">The desired secure password.</param>
/// <param name="StoreName">Optional name of the business/store.</param>
public sealed record RegisterRequest(string Email, string Password, string? StoreName = null);

/// <summary>
/// Request contract for login authentication.
/// </summary>
/// <param name="Email">The user's email address.</param>
/// <param name="Password">The user's password.</param>
public sealed record LoginRequest(string Email, string Password);
