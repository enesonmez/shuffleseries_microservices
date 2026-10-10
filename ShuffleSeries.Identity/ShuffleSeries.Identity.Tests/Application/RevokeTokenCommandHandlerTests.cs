using Moq;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.RevokeToken;
using ShuffleSeries.Identity.Application.Interfaces;
using ShuffleSeries.Identity.Domain.Entities;
using ShuffleSeries.Identity.Domain.Exceptions;
using ShuffleSeries.Identity.Domain.Repositories;
using ShuffleSeries.Shared.Core.Application.Security;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Identity.Tests.Application;

public class RevokeTokenCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<ITokenBlacklistService> _blacklistServiceMock = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly RevokeTokenCommandHandler _handler;

    public RevokeTokenCommandHandlerTests()
    {
        _handler = new RevokeTokenCommandHandler(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _tokenServiceMock.Object,
            _timeProvider,
            _blacklistServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenTokenNotFound_ShouldThrowTokenNotFoundException()
    {
        // Arrange
        const string rawToken = "non_existent_token";
        const string hashedToken = "hashed_non_existent";

        _tokenServiceMock.Setup(t => t.HashToken(rawToken)).Returns(hashedToken);
        _userRepositoryMock.Setup(r => r.GetByRefreshTokenHashAsync(hashedToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var act = async () => await _handler.Handle(new RevokeTokenCommand(rawToken), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<TokenNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenValidRefreshTokenOnly_ShouldRevokeRefreshToken_AndNotCallBlacklist()
    {
        // Arrange
        const string rawToken = "valid_token";
        const string hashedToken = "hashed_valid";

        var user = User.CreateStandard("user@test.com", "hash");
        user.AddRefreshToken(hashedToken, DateTime.UtcNow.AddDays(7));

        _tokenServiceMock.Setup(t => t.HashToken(rawToken)).Returns(hashedToken);
        _userRepositoryMock.Setup(r => r.GetByRefreshTokenHashAsync(hashedToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(new RevokeTokenCommand(rawToken), CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        user.RefreshTokens.First().IsRevoked.Should().BeTrue();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _blacklistServiceMock.Verify(b => b.BlacklistTokenAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenJwtIdProvided_ShouldRevokeRefreshToken_AndBlacklistTokenInRedis()
    {
        // Arrange
        const string rawToken = "valid_token";
        const string hashedToken = "hashed_valid";
        const string jwtId = "jwt-id-12345";

        var user = User.CreateStandard("user@test.com", "hash");
        user.AddRefreshToken(hashedToken, DateTime.UtcNow.AddDays(7));

        _tokenServiceMock.Setup(t => t.HashToken(rawToken)).Returns(hashedToken);
        _userRepositoryMock.Setup(r => r.GetByRefreshTokenHashAsync(hashedToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(new RevokeTokenCommand(rawToken, JwtId: jwtId), CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _blacklistServiceMock.Verify(b => b.BlacklistTokenAsync(jwtId, It.IsAny<TimeSpan>(), "logout", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenAccessTokenProvided_ShouldExtractTokenInfo_AndBlacklistInRedis()
    {
        // Arrange
        const string rawToken = "valid_token";
        const string hashedToken = "hashed_valid";
        const string accessToken = "raw_jwt_access_token";
        const string extractedJti = "extracted-jti-999";
        var expiry = DateTime.UtcNow.AddMinutes(20);

        var user = User.CreateStandard("user@test.com", "hash");
        user.AddRefreshToken(hashedToken, DateTime.UtcNow.AddDays(7));

        _tokenServiceMock.Setup(t => t.HashToken(rawToken)).Returns(hashedToken);
        _userRepositoryMock.Setup(r => r.GetByRefreshTokenHashAsync(hashedToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _tokenServiceMock.Setup(t => t.ExtractTokenInfo(accessToken))
            .Returns((extractedJti, expiry));

        // Act
        var result = await _handler.Handle(new RevokeTokenCommand(rawToken, AccessToken: accessToken), CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _blacklistServiceMock.Verify(b => b.BlacklistTokenAsync(
            extractedJti,
            It.Is<TimeSpan>(ttl => ttl > TimeSpan.FromMinutes(15) && ttl <= TimeSpan.FromMinutes(21)),
            "logout",
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
