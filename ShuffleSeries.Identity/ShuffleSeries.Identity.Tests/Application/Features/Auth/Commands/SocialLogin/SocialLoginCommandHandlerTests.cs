using Moq;
using ShuffleSeries.Identity.Application.Common.Interfaces;
using ShuffleSeries.Identity.Application.Common.Models;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.SocialLogin;
using ShuffleSeries.Identity.Domain.Entities;
using ShuffleSeries.Identity.Domain.Exceptions;
using ShuffleSeries.Identity.Domain.Repositories;
using ShuffleSeries.Shared.Core.Application.Security;

namespace ShuffleSeries.Identity.Tests.Application.Features.Auth.Commands.SocialLogin;

public class SocialLoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IRoleRepository> _roleRepositoryMock = new();
    private readonly Mock<IExternalAuthService> _externalAuthServiceMock = new();
    private readonly Mock<IAuthResponseService> _authResponseServiceMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly Mock<ITokenBlacklistService> _blacklistServiceMock = new();
    private readonly SocialLoginCommandHandler _handler;

    public SocialLoginCommandHandlerTests()
    {
        _handler = new SocialLoginCommandHandler(
            _userRepositoryMock.Object,
            _roleRepositoryMock.Object,
            _externalAuthServiceMock.Object,
            _authResponseServiceMock.Object,
            _tokenServiceMock.Object,
            _timeProvider,
            _blacklistServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenTokenInvalid_ShouldThrowInvalidExternalTokenException()
    {
        // Arrange
        _externalAuthServiceMock.Setup(s => s.VerifyTokenAsync("Google", "bad_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExternalUserPrincipal?)null);

        var command = new SocialLoginCommand("Google", "bad_token");

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<InvalidExternalTokenException>();
        ex.Which.Code.Should().Be("INVALID_EXTERNAL_TOKEN");
    }

    [Fact]
    public async Task Handle_WhenNewSocialUser_ShouldCreateUser_AssignRole_AndReturnAuthResponse()
    {
        // Arrange
        var principal = new ExternalUserPrincipal("Google", "sub_123", "new_social@gmail.com", "Google User");
        var role = Role.Create(SystemRoles.Standard, "Standard");

        _externalAuthServiceMock.Setup(s => s.VerifyTokenAsync("Google", "valid_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(principal);
        _userRepositoryMock.Setup(r => r.GetByLoginAsync("Google", "sub_123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(r => r.GetByEmailAsync("new_social@gmail.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _roleRepositoryMock.Setup(r => r.GetDefaultRoleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        var expectedResponse = new AuthResponse(
            Guid.NewGuid(),
            "new_social@gmail.com",
            false,
            [SystemRoles.Standard],
            [SystemPermissions.Catalog.MoviesRead],
            "social_jwt_token",
            "raw_refresh_token",
            3600);

        _authResponseServiceMock.Setup(a => a.CreateAuthResponseAsync(It.IsAny<User>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var command = new SocialLoginCommand("Google", "valid_token");

        // Act
        var response = await _handler.Handle(command, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.Email.Should().Be("new_social@gmail.com");
        response.AccessToken.Should().Be("social_jwt_token");
        _userRepositoryMock.Verify(r => r.Add(It.IsAny<User>()), Times.Once);
        _authResponseServiceMock.Verify(a => a.CreateAuthResponseAsync(It.IsAny<User>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenExistingUserWithMatchingEmail_ShouldLinkAccount_AndReturnAuthResponse()
    {
        // Arrange
        var existingUser = User.CreateStandard("existing@test.com", "hash");
        var principal = new ExternalUserPrincipal("Apple", "apple_sub_456", "existing@test.com", "Apple User");

        _externalAuthServiceMock.Setup(s => s.VerifyTokenAsync("Apple", "apple_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(principal);
        _userRepositoryMock.Setup(r => r.GetByLoginAsync("Apple", "apple_sub_456", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(r => r.GetByEmailWithLoginsAsync("existing@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        var expectedResponse = new AuthResponse(
            existingUser.Id,
            "existing@test.com",
            false,
            [SystemRoles.Standard],
            [SystemPermissions.Catalog.MoviesRead],
            "linked_jwt_token",
            "raw_refresh_token",
            3600);

        _authResponseServiceMock.Setup(a => a.CreateAuthResponseAsync(existingUser, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var command = new SocialLoginCommand("Apple", "apple_token");

        // Act
        var response = await _handler.Handle(command, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        existingUser.UserLogins.Should().Contain(ul => ul.Provider == "Apple" && ul.ProviderKey == "apple_sub_456");
        _userRepositoryMock.Verify(r => r.Add(It.IsAny<User>()), Times.Never);
        _authResponseServiceMock.Verify(a => a.CreateAuthResponseAsync(existingUser, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenExistingUserIsSuspended_ShouldThrowInvalidCredentialsException()
    {
        // Arrange
        var suspendedUser = User.CreateStandard("suspended@test.com", "hash");
        suspendedUser.Suspend("Policy violation");
        var principal = new ExternalUserPrincipal("Google", "google_sub_789", "suspended@test.com", "Suspended User");

        _externalAuthServiceMock.Setup(s => s.VerifyTokenAsync("Google", "valid_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(principal);
        _userRepositoryMock.Setup(r => r.GetByLoginAsync("Google", "google_sub_789", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(r => r.GetByEmailWithLoginsAsync("suspended@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(suspendedUser);

        var command = new SocialLoginCommand("Google", "valid_token");

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<InvalidCredentialsException>();
        ex.Which.Code.Should().Be("INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Handle_WhenFirstSocialLinkOnUserWithPriorTokens_ShouldRevokeAllPriorRefreshTokens_AndCallBlacklist()
    {
        // Arrange
        var existingUser = User.CreateStandard("existing@test.com", "hash");
        var token = existingUser.AddRefreshToken("prior_hash", DateTime.UtcNow.AddDays(5));
        var principal = new ExternalUserPrincipal("Apple", "apple_sub_456", "existing@test.com", "Apple User");

        _externalAuthServiceMock.Setup(s => s.VerifyTokenAsync("Apple", "apple_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(principal);
        _userRepositoryMock.Setup(r => r.GetByLoginAsync("Apple", "apple_sub_456", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(r => r.GetByEmailWithLoginsAsync("existing@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        var expectedResponse = new AuthResponse(
            existingUser.Id,
            "existing@test.com",
            false,
            [SystemRoles.Standard],
            [],
            "new_jwt",
            "new_refresh",
            3600);

        _authResponseServiceMock.Setup(a => a.CreateAuthResponseAsync(existingUser, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var command = new SocialLoginCommand("Apple", "apple_token");

        // Act
        var response = await _handler.Handle(command, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        token.IsRevoked.Should().BeTrue();
        existingUser.PasswordHash.Should().BeNull();
        _blacklistServiceMock.Verify(b => b.BlacklistUserTokensAsync(
            existingUser.Id,
            It.IsAny<TimeSpan>(),
            It.IsAny<DateTimeOffset?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
