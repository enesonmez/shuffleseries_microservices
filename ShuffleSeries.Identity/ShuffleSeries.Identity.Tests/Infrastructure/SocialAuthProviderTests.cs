using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Moq;
using ShuffleSeries.Identity.Application.Interfaces;
using ShuffleSeries.Identity.Application.Models;
using ShuffleSeries.Identity.Infrastructure.Configuration;
using ShuffleSeries.Identity.Infrastructure.Services;
using ShuffleSeries.Identity.Infrastructure.Services.Social;

namespace ShuffleSeries.Identity.Tests.Infrastructure;

public class SocialAuthProviderTests
{
    private readonly HttpClient _httpClient = new();
    private readonly IOptions<ExternalAuthOptions> _options = Options.Create(new ExternalAuthOptions
    {
        ValidateSignatures = false,
        Google = new GoogleAuthOptions { ClientId = "test-google-client" },
        Apple = new AppleAuthOptions { ClientId = "test-apple-client" }
    });

    [Fact]
    public async Task GoogleAuthProvider_WhenGivenMockToken_ReturnsValidPrincipal()
    {
        // Arrange
        var provider = new GoogleAuthProvider(_httpClient, _options);

        // Act
        var result = await provider.ValidateTokenAsync("mock_google_123:test@gmail.com");

        // Assert
        result.Should().NotBeNull();
        result!.Provider.Should().Be("Google");
        result.SubjectId.Should().Be("google_123");
        result.Email.Should().Be("test@gmail.com");
    }

    [Fact]
    public async Task AppleAuthProvider_WhenGivenMockToken_ReturnsValidPrincipal()
    {
        // Arrange
        var provider = new AppleAuthProvider(_httpClient, _options);

        // Act
        var result = await provider.ValidateTokenAsync("mock_apple_777:user@privaterelay.appleid.com");

        // Assert
        result.Should().NotBeNull();
        result!.Provider.Should().Be("Apple");
        result.SubjectId.Should().Be("apple_777");
        result.Email.Should().Be("user@privaterelay.appleid.com");
    }

    [Fact]
    public async Task ExternalAuthService_WhenProviderMatches_DispatchesToCorrectProvider()
    {
        // Arrange
        var googleMock = new Mock<ISocialAuthProvider>();
        googleMock.Setup(p => p.Provider).Returns("Google");
        googleMock.Setup(p => p.ValidateTokenAsync("valid_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalUserPrincipal("Google", "sub123", "g@test.com", "Google User"));

        var appleMock = new Mock<ISocialAuthProvider>();
        appleMock.Setup(p => p.Provider).Returns("Apple");

        var service = new ExternalAuthService([googleMock.Object, appleMock.Object]);

        // Act
        var result = await service.VerifyTokenAsync("google", "valid_token");

        // Assert
        result.Should().NotBeNull();
        result!.Provider.Should().Be("Google");
        result.SubjectId.Should().Be("sub123");
        googleMock.Verify(p => p.ValidateTokenAsync("valid_token", It.IsAny<CancellationToken>()), Times.Once);
        appleMock.Verify(p => p.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExternalAuthService_WhenProviderNotFound_ReturnsNull()
    {
        // Arrange
        var googleMock = new Mock<ISocialAuthProvider>();
        googleMock.Setup(p => p.Provider).Returns("Google");

        var service = new ExternalAuthService([googleMock.Object]);

        // Act
        var result = await service.VerifyTokenAsync("UnknownProvider", "any_token");

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("", "token")]
    [InlineData("Google", "")]
    [InlineData(null, "token")]
    [InlineData("Google", null)]
    public async Task ExternalAuthService_WhenInputsInvalid_ReturnsNull(string? provider, string? token)
    {
        // Arrange
        var service = new ExternalAuthService([]);

        // Act
        var result = await service.VerifyTokenAsync(provider!, token!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GoogleAuthProvider_WhenJwtHasEmailVerifiedFalse_ReturnsNull()
    {
        // Arrange
        var provider = new GoogleAuthProvider(_httpClient, _options);
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateJwtSecurityToken(
            subject: new ClaimsIdentity(
            [
                new Claim("sub", "google_unverified_1"),
                new Claim("email", "unverified@gmail.com"),
                new Claim("email_verified", "false")
            ]));
        var jwtString = tokenHandler.WriteToken(token);

        // Act
        var result = await provider.ValidateTokenAsync(jwtString);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GoogleAuthProvider_WhenJwtHasEmailVerifiedTrue_ReturnsValidPrincipal()
    {
        // Arrange
        var provider = new GoogleAuthProvider(_httpClient, _options);
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateJwtSecurityToken(
            subject: new ClaimsIdentity(
            [
                new Claim("sub", "google_verified_1"),
                new Claim("email", "verified@gmail.com"),
                new Claim("email_verified", "true")
            ]));
        var jwtString = tokenHandler.WriteToken(token);

        // Act
        var result = await provider.ValidateTokenAsync(jwtString);

        // Assert
        result.Should().NotBeNull();
        result!.Provider.Should().Be("Google");
        result.SubjectId.Should().Be("google_verified_1");
        result.Email.Should().Be("verified@gmail.com");
    }
}
