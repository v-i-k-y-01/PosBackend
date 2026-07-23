using PosBackend.Application.Auth.Dtos;
using PosBackend.Domain.Entities;

namespace PosBackend.Application.Common.Interfaces;

public interface ITokenService
{
    TokenResponse CreateTokens(User user);
}
