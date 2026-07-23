namespace PosBackend.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid UserId { get; }
    bool IsOwner { get; }
}
