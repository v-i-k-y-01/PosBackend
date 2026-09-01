using PosBackend.Application.Common.Interfaces;

namespace PosBackend.UnitTests.Common;

public class TestCurrentUserService : ICurrentUserService
{
    public Guid UserId { get; set; } = Guid.NewGuid();
    public Guid StoreId { get; set; } = Guid.NewGuid();
    public bool IsOwner { get; set; } = true;
}
