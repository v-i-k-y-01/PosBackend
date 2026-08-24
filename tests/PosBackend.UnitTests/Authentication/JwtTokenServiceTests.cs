using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using PosBackend.Domain.Entities;
using PosBackend.Domain.Enums;
using PosBackend.Infrastructure.Authentication;
using Xunit;

namespace PosBackend.UnitTests.Authentication;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _tokenService;
    private readonly JwtOptions _options;

    public JwtTokenServiceTests()
    {
        _options = new JwtOptions
        {
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            Key = "SuperSecretSigningKeyMustBeVeryLong32Chars!",
            ExpiryMinutes = 30,
            RefreshTokenExpiryDays = 5
        };

        var optionsMock = new Mock<IOptions<JwtOptions>>();
        optionsMock.Setup(x => x.Value).Returns(_options);

        _tokenService = new JwtTokenService(optionsMock.Object);
    }

    [Fact]
    public void CreateTokens_ShouldGenerateValidTokensWithCorrectClaims()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Role = UserRole.Owner
        };

        // Act
        var tokenResponse = _tokenService.CreateTokens(user);

        // Assert
        tokenResponse.Should().NotBeNull();
        tokenResponse.AccessToken.Should().NotBeNullOrWhiteSpace();
        tokenResponse.RefreshToken.Should().NotBeNullOrWhiteSpace();

        // Decode token to verify claims
        var tokenHandler = new JwtSecurityTokenHandler();
        
        var parsedAccessToken = tokenHandler.ReadJwtToken(tokenResponse.AccessToken);
        parsedAccessToken.Issuer.Should().Be(_options.Issuer);
        parsedAccessToken.Audiences.Should().Contain(_options.Audience);
        
        var subClaim = parsedAccessToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value;
        subClaim.Should().Be(user.Id.ToString());

        var emailClaim = parsedAccessToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value;
        emailClaim.Should().Be(user.Email);

        var roleClaim = parsedAccessToken.Claims.First(c => c.Type == ClaimTypes.Role).Value;
        roleClaim.Should().Be(user.Role.ToString());

        var typeClaim = parsedAccessToken.Claims.First(c => c.Type == "token_type").Value;
        typeClaim.Should().Be("access");

        // Verify Refresh Token
        var parsedRefreshToken = tokenHandler.ReadJwtToken(tokenResponse.RefreshToken);
        var refreshTypeClaim = parsedRefreshToken.Claims.First(c => c.Type == "token_type").Value;
        refreshTypeClaim.Should().Be("refresh");
    }
}
