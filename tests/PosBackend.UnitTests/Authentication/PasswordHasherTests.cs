using FluentAssertions;
using PosBackend.Infrastructure.Authentication;
using Xunit;

namespace PosBackend.UnitTests.Authentication;

public class PasswordHasherTests
{
    private readonly BCryptPasswordHasher _hasher;

    public PasswordHasherTests()
    {
        _hasher = new BCryptPasswordHasher();
    }

    [Fact]
    public void Hash_ShouldReturnNonNullOrEmptyHash()
    {
        // Arrange
        var password = "SecurePassword123!";

        // Act
        var hash = _hasher.Hash(password);

        // Assert
        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().StartWith("$2a$"); // BCrypt prefix
    }

    [Fact]
    public void Verify_ShouldReturnTrue_WhenPasswordMatchesHash()
    {
        // Arrange
        var password = "MySecretPassword";
        var hash = _hasher.Hash(password);

        // Act
        var result = _hasher.Verify(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Verify_ShouldReturnFalse_WhenPasswordDoesNotMatchHash()
    {
        // Arrange
        var password = "MySecretPassword";
        var wrongPassword = "WrongPassword";
        var hash = _hasher.Hash(password);

        // Act
        var result = _hasher.Verify(wrongPassword, hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_ShouldReturnFalse_WhenHashIsMalformed()
    {
        // Arrange
        var password = "password";
        var malformedHash = "invalid_hash_value";

        // Act
        var result = _hasher.Verify(password, malformedHash);

        // Assert
        result.Should().BeFalse();
    }
}
