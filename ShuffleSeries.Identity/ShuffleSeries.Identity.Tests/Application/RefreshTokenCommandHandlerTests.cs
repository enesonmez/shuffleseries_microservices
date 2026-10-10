using Moq;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.RefreshToken;
using ShuffleSeries.Identity.Application.Interfaces;
using ShuffleSeries.Identity.Application.Models;
using ShuffleSeries.Identity.Domain.Entities;
using ShuffleSeries.Identity.Domain.Exceptions;
using ShuffleSeries.Identity.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Identity.Tests.Application;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<IPermissionResolver> _permissionResolverMock = new();
    private readonly Mock<ShuffleSeries.Shared.Core.Application.Security.ITokenBlacklistService> _blacklistServiceMock = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _handler = new RefreshTokenCommandHandler(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _tokenServiceMock.Object,
            _permissionResolverMock.Object,
            _timeProvider,
            _blacklistServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenTokenIsRevoked_ShouldRevokeAllSessions_BlacklistInRedis_AndThrowUnauthorizedException()
    {
        // Arrange
        const string rawToken = "compromised_token";
        const string hashedToken = "hashed_compromised_token";

        var user = User.CreateStandard("victim@test.com", "hash");
        var token = user.AddRefreshToken(hashedToken, DateTime.UtcNow.AddDays(7));
        token.Revoke(); // Already revoked token

        _tokenServiceMock.Setup(t => t.HashToken(rawToken)).Returns(hashedToken);
        _userRepositoryMock.Setup(r => r.GetByRefreshTokenHashAsync(hashedToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var act = async () => await _handler.Handle(new RefreshTokenCommand(rawToken), CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<TokenCompromisedException>();
        ex.Which.Code.Should().Be("TOKEN_REUSE_DETECTED");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _blacklistServiceMock.Verify(b => b.BlacklistUserTokensAsync(user.Id, TimeSpan.FromHours(1), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTokenIsValid_ShouldRotateToken_AndReturnNewTokens()
    {
        // Arrange
        const string rawToken = "valid_token";
        const string hashedToken = "hashed_valid_token";

        var user = User.CreateStandard("user@test.com", "hash");
        user.AddRefreshToken(hashedToken, DateTime.UtcNow.AddDays(7));

        _tokenServiceMock.Setup(t => t.HashToken(rawToken)).Returns(hashedToken);
        _tokenServiceMock.Setup(t => t.GenerateRefreshToken()).Returns("new_raw_token");
        _tokenServiceMock.Setup(t => t.HashToken("new_raw_token")).Returns("new_hashed_token");
        _tokenServiceMock.Setup(t => t.GenerateTokensAsync(It.IsAny<User>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenResponse("new_access_jwt", "new_raw_token", 3600));

        _userRepositoryMock.Setup(r => r.GetByRefreshTokenHashAsync(hashedToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(new RefreshTokenCommand(rawToken), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("new_access_jwt");
        result.RefreshToken.Should().Be("new_raw_token");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
