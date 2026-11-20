using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using ShuffleSeries.Identity.Application.Common.Interfaces;
using ShuffleSeries.Identity.Application.Common.Models;
using ShuffleSeries.Identity.Infrastructure.Configuration;
using ShuffleSeries.Identity.Infrastructure.Services;
using ShuffleSeries.Identity.Infrastructure.Services.Social;

namespace ShuffleSeries.Identity.Tests.Infrastructure.Services;

public class SocialAuthProviderTests
{
    private readonly IHttpClientFactory _httpClientFactory = new TestHttpClientFactory(new HttpClient());
    private readonly IHostEnvironment _testEnvironment = CreateEnvironment("Testing");
    private readonly IOptions<ExternalAuthOptions> _options = Options.Create(new ExternalAuthOptions
    {
        ValidateSignatures = false,
        Google = new GoogleAuthOptions { ClientId = "test-google-client" },
        Apple = new AppleAuthOptions { ClientId = "test-apple-client" }
    });

    private static IHostEnvironment CreateEnvironment(string environmentName)
    {
        var mock = new Mock<IHostEnvironment>();
        mock.Setup(e => e.EnvironmentName).Returns(environmentName);
        return mock.Object;
    }

    private GoogleAuthProvider CreateGoogleProvider(IHostEnvironment? environment = null) =>
        new(_httpClientFactory, _options, environment: environment ?? _testEnvironment);

    private AppleAuthProvider CreateAppleProvider(IHostEnvironment? environment = null) =>
        new(_httpClientFactory, _options, environment: environment ?? _testEnvironment);

    private sealed class TestHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    [Fact]
    public async Task GoogleAuthProvider_WhenGivenMockToken_ReturnsValidPrincipal()
    {
        // Arrange
        var provider = CreateGoogleProvider();

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
        var provider = CreateAppleProvider();

        // Act
        var result = await provider.ValidateTokenAsync("mock_apple_777:user@privaterelay.appleid.com");

        // Assert
        result.Should().NotBeNull();
        result!.Provider.Should().Be("Apple");
        result.SubjectId.Should().Be("apple_777");
        result.Email.Should().Be("user@privaterelay.appleid.com");
    }

    [Fact]
    public async Task AppleAuthProvider_WhenJwtHasEmailVerifiedFalse_ReturnsNull()
    {
        // Arrange
        var provider = CreateAppleProvider();
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateJwtSecurityToken(
            subject: new ClaimsIdentity(
            [
                new Claim("sub", "apple_unverified_1"),
                new Claim("email", "unverified@privaterelay.appleid.com"),
                new Claim("email_verified", "false")
            ]));
        var jwtString = tokenHandler.WriteToken(token);

        // Act
        var result = await provider.ValidateTokenAsync(jwtString);

        // Assert
        result.Should().BeNull();
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
        var provider = CreateGoogleProvider();
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
        var provider = CreateGoogleProvider();
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

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task ValidateTokenAsync_WhenTokenIsNullOrWhitespace_ReturnsNull(string? token)
    {
        // Arrange
        var provider = CreateGoogleProvider();

        // Act
        var result = await provider.ValidateTokenAsync(token!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateTokenAsync_WhenTokenIsUnreadableAndDoesNotStartWithMock_ReturnsNull()
    {
        // Arrange
        var provider = CreateGoogleProvider();

        // Act
        var result = await provider.ValidateTokenAsync("random_unreadable_token_not_mock");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateTokenAsync_WhenMockTokenHasNoColon_ReturnsPrincipalWithDefaultEmail()
    {
        // Arrange
        var provider = CreateGoogleProvider();

        // Act
        var result = await provider.ValidateTokenAsync("mock_subject_without_email");

        // Assert
        result.Should().NotBeNull();
        result!.Provider.Should().Be("Google");
        result.SubjectId.Should().Be("subject_without_email");
        result.Email.Should().Be("subject_without_email@google.com");
    }

    [Fact]
    public async Task ValidateTokenAsync_WhenMockTokenHasThreeParts_ReturnsPrincipalWithParsedEmail()
    {
        // Arrange
        var provider = CreateGoogleProvider();

        // Act
        var result = await provider.ValidateTokenAsync("mock:custom_sub_123:custom@google.com");

        // Assert
        result.Should().NotBeNull();
        result!.Provider.Should().Be("Google");
        result.SubjectId.Should().Be("custom_sub_123");
        result.Email.Should().Be("custom@google.com");
    }

    [Fact]
    public async Task ValidateTokenAsync_WhenReadableJwtLacksSubClaim_ReturnsNull()
    {
        // Arrange
        var provider = CreateGoogleProvider();
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateJwtSecurityToken(
            subject: new ClaimsIdentity(
            [
                new Claim("email", "nosub@gmail.com")
            ]));
        var jwtString = tokenHandler.WriteToken(token);

        // Act
        var result = await provider.ValidateTokenAsync(jwtString);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("QA")]
    [InlineData("PreProd")]
    [InlineData(null)]
    public async Task ValidateTokenAsync_WhenValidateSignaturesIsFalseAndEnvironmentIsNotDevOrTesting_ThrowsInvalidOperationException(string? envName)
    {
        // Arrange
        IHostEnvironment? env = null;
        if (envName is not null)
        {
            var mock = new Mock<IHostEnvironment>();
            mock.Setup(e => e.EnvironmentName).Returns(envName);
            env = mock.Object;
        }

        var provider = new GoogleAuthProvider(
            _httpClientFactory,
            _options,
            timeProvider: null,
            environment: env);

        // Act
        var act = async () => await provider.ValidateTokenAsync("mock:attacker:admin@shuffleseries.com");

        // Assert
        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.Which.Message.Should().Contain("bypass is forbidden outside development or testing environments");
    }

    [Fact]
    public void Dispose_ShouldReleaseResourcesWithoutThrowing()
    {
        var provider = CreateGoogleProvider();

        var act = () =>
        {
            provider.Dispose();
            provider.Dispose();
        };

        act.Should().NotThrow();
    }

    [Fact]
    public async Task ValidateTokenAsync_WhenValidateSignaturesIsTrueAndClientIdMissing_ShouldReturnNull()
    {
        var strictOptions = Options.Create(new ExternalAuthOptions
        {
            ValidateSignatures = true
        });
        using var provider = new GoogleAuthProvider(_httpClientFactory, strictOptions, environment: CreateEnvironment("Production"));

        var result = await provider.ValidateTokenAsync("some.jwt.token");

        result.Should().BeNull();
    }

    [Theory]
    [InlineData("mock_sub123:user@example.com", "sub123", "user@example.com")]
    [InlineData("mock:custom@example.com", "mock", "custom@example.com")]
    public async Task ValidateTokenAsync_WhenMockTokenHasTwoParts_ShouldParseSubjectAndEmail(
        string mockToken,
        string expectedSubject,
        string expectedEmail)
    {
        using var provider = CreateGoogleProvider();

        var result = await provider.ValidateTokenAsync(mockToken);

        result.Should().NotBeNull();
        result!.SubjectId.Should().Be(expectedSubject);
        result.Email.Should().Be(expectedEmail);
    }
}
