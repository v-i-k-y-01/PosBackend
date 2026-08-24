using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PosBackend.Application.Auth.Dtos;
using PosBackend.Application.Users.Commands;

namespace PosBackend.Api.Controllers;

/// <summary>
/// API endpoints for managing users (Owner/Cashier creation).
/// Access is restricted to users with the Owner role.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize(Roles = "Owner")]
public sealed class UsersController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes a new instance of the <see cref="UsersController"/> class.
    /// </summary>
    /// <param name="sender">The MediatR sender utility.</param>
    public UsersController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Creates an additional user with the Owner role.
    /// </summary>
    /// <param name="request">Payload containing email and password credentials.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>Details of the newly created Owner user.</returns>
    [HttpPost("owners")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> CreateOwner(CreateOwnerRequest request, CancellationToken cancellationToken)
    {
        var ownerUser = await _sender.Send(
            new CreateOwnerCommand(request.Email, request.Password),
            cancellationToken);

        return Created(string.Empty, ownerUser);
    }

    /// <summary>
    /// Creates a user with the Cashier role.
    /// </summary>
    /// <param name="request">Payload containing email and password credentials.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>Details of the newly created Cashier user.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> CreateCashier(CreateCashierRequest request, CancellationToken cancellationToken)
    {
        var cashierUser = await _sender.Send(
            new CreateCashierCommand(request.Email, request.Password),
            cancellationToken);

        return Created(string.Empty, cashierUser);
    }
}

/// <summary>
/// Request payload contract for creating a Cashier user.
/// </summary>
/// <param name="Email">The cashier's email address.</param>
/// <param name="Password">The temporary cashier password.</param>
public sealed record CreateCashierRequest(string Email, string Password);

/// <summary>
/// Request payload contract for creating an Owner user.
/// </summary>
/// <param name="Email">The owner's email address.</param>
/// <param name="Password">The temporary owner password.</param>
public sealed record CreateOwnerRequest(string Email, string Password);
