using MediatR;
using Microsoft.AspNetCore.Mvc;
using PosBackend.Application.Auth.Commands;
using PosBackend.Application.Auth.Dtos;

namespace PosBackend.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<UserResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var user = await sender.Send(new RegisterOwnerCommand(request.Email, request.Password), cancellationToken);
        return Created(string.Empty, user);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TokenResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new LoginCommand(request.Email, request.Password), cancellationToken));
    }
}

public sealed record RegisterRequest(string Email, string Password);
public sealed record LoginRequest(string Email, string Password);
