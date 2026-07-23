using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosBackend.Application.Auth.Dtos;
using PosBackend.Application.Users.Commands;

namespace PosBackend.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Owner")]
public sealed class UsersController(ISender sender) : ControllerBase
{
    [HttpPost("owners")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<UserResponse>> CreateOwner(CreateOwnerRequest request, CancellationToken cancellationToken)
    {
        var user = await sender.Send(new CreateOwnerCommand(request.Email, request.Password), cancellationToken);
        return Created(string.Empty, user);
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<UserResponse>> CreateCashier(CreateCashierRequest request, CancellationToken cancellationToken)
    {
        var user = await sender.Send(new CreateCashierCommand(request.Email, request.Password), cancellationToken);
        return Created(string.Empty, user);
    }
}

public sealed record CreateCashierRequest(string Email, string Password);
public sealed record CreateOwnerRequest(string Email, string Password);
