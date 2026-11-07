using Moq;
using ShuffleSeries.Identity.Application.Common.Interfaces;
using ShuffleSeries.Identity.Application.Common.Services;
using ShuffleSeries.Identity.Domain.Entities;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Identity.Tests.Application.Common.Services;

public class AuthResponseServiceTests
{
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IPermissionResolver> _permissionResolverMock = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly AuthResponseService _service;

    public AuthResponseServiceTests()
    {
        _tokenServiceMock.Setup(t => t.RefreshTokenLifetime).Returns(TimeSpan.FromDays(14));
        _tokenServiceMock.Setup(t => t.GuestRefreshTokenLifetime).Returns(TimeSpan.FromDays(30));
        _tokenServiceMock.Setup(t => t.GenerateRefreshToken()).Returns("raw_refresh_token_123");
        _tokenServiceMock.Setup(t => t.HashToken("raw_refresh_token_123")).Returns("hashed_refresh_token_123");

        _service = new AuthResponseService(
            _tokenServiceMock.Object,
            _unitOfWorkMock.Object,
            _permissionResolverMock.Object,
            _timeProvider);
    }

    [Fact]
    public async Task CreateAuthResponseAsync_WhenStandardUser_ShouldUseStandardLifetime_AndPersistRefreshToken()
    {
        // Arrange
        var user = User.CreateStandard("user@test.com", "pass_hash");
        _permissionResolverMock.Setup(p => p.ResolveEffectivePermissionsAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(([SystemRoles.Standard], [SystemPermissions.Catalog.MoviesRead]));
        _tokenServiceMock.Setup(t => t.GenerateTokensAsync(user, It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenResponse("access_jwt", "raw_refresh_token_123", 3600));

        // Act
        var result = await _service.CreateAuthResponseAsync(user, "192.168.1.50", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(user.Id);
        result.Email.Should().Be("user@test.com");
        result.IsGuest.Should().BeFalse();
        result.AccessToken.Should().Be("access_jwt");
        result.RefreshToken.Should().Be("raw_refresh_token_123");
        result.Roles.Should().Contain(SystemRoles.Standard);
        result.Permissions.Should().Contain(SystemPermissions.Catalog.MoviesRead);

        user.RefreshTokens.Should().ContainSingle(rt => rt.TokenHash == "hashed_refresh_token_123" && rt.CreatedByIp == "192.168.1.50");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAuthResponseAsync_WhenGuestUser_ShouldUseGuestLifetime_AndPersistRefreshToken()
    {
        // Arrange
        var user = User.CreateGuest();
        _permissionResolverMock.Setup(p => p.ResolveEffectivePermissionsAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(([SystemRoles.Guest], [SystemPermissions.Catalog.MoviesRead]));
        _tokenServiceMock.Setup(t => t.GenerateTokensAsync(user, It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenResponse("guest_access_jwt", "raw_refresh_token_123", 3600));

        // Act
        var result = await _service.CreateAuthResponseAsync(user, "10.0.0.1", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(user.Id);
        result.IsGuest.Should().BeTrue();
        result.AccessToken.Should().Be("guest_access_jwt");
        result.Roles.Should().Contain(SystemRoles.Guest);

        user.RefreshTokens.Should().ContainSingle(rt => rt.TokenHash == "hashed_refresh_token_123" && rt.CreatedByIp == "10.0.0.1");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
