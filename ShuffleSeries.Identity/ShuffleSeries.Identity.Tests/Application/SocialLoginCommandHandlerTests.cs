using Moq;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.SocialLogin;
using ShuffleSeries.Identity.Application.Interfaces;
using ShuffleSeries.Identity.Application.Models;
using ShuffleSeries.Identity.Domain.Entities;
using ShuffleSeries.Identity.Domain.Exceptions;
using ShuffleSeries.Identity.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Constants;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Identity.Tests.Application;

public class SocialLoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IRoleRepository> _roleRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IExternalAuthService> _externalAuthServiceMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<IPermissionResolver> _permissionResolverMock = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly SocialLoginCommandHandler _handler;

    public SocialLoginCommandHandlerTests()
    {
        _handler = new SocialLoginCommandHandler(
            _userRepositoryMock.Object,
            _roleRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _externalAuthServiceMock.Object,
            _tokenServiceMock.Object,
            _permissionResolverMock.Object,
            _timeProvider);
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
        _tokenServiceMock.Setup(t => t.GenerateRefreshToken())
            .Returns("raw_refresh_token");
        _tokenServiceMock.Setup(t => t.HashToken(It.IsAny<string>()))
            .Returns("hashed_refresh_token");
        _permissionResolverMock.Setup(p => p.ResolveEffectivePermissionsAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(([SystemRoles.Standard], ["catalog:read"]));
        _tokenServiceMock.Setup(t => t.GenerateTokensAsync(It.IsAny<User>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenResponse("social_jwt_token", "raw_refresh_token", 3600));

        var command = new SocialLoginCommand("Google", "valid_token");

        // Act
        var response = await _handler.Handle(command, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.Email.Should().Be("new_social@gmail.com");
        response.AccessToken.Should().Be("social_jwt_token");
        _userRepositoryMock.Verify(r => r.Add(It.IsAny<User>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
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
        _userRepositoryMock.Setup(r => r.GetByEmailAsync("existing@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        _tokenServiceMock.Setup(t => t.GenerateRefreshToken())
            .Returns("raw_refresh_token");
        _tokenServiceMock.Setup(t => t.HashToken(It.IsAny<string>()))
            .Returns("hashed_refresh_token");
        _permissionResolverMock.Setup(p => p.ResolveEffectivePermissionsAsync(existingUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(([SystemRoles.Standard], ["catalog:read"]));
        _tokenServiceMock.Setup(t => t.GenerateTokensAsync(existingUser, It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenResponse("linked_jwt_token", "raw_refresh_token", 3600));

        var command = new SocialLoginCommand("Apple", "apple_token", "apple_refresh_token");

        // Act
        var response = await _handler.Handle(command, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        existingUser.UserLogins.Should().Contain(ul => ul.Provider == "Apple" && ul.ProviderKey == "apple_sub_456");
        _userRepositoryMock.Verify(r => r.Add(It.IsAny<User>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
