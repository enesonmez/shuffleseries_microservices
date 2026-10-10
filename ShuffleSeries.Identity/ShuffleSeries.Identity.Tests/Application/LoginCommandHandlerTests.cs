using Moq;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.Login;
using ShuffleSeries.Identity.Application.Interfaces;
using ShuffleSeries.Identity.Application.Models;
using ShuffleSeries.Identity.Domain.Entities;
using ShuffleSeries.Identity.Domain.Exceptions;
using ShuffleSeries.Identity.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Constants;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Identity.Tests.Application;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<IPermissionResolver> _permissionResolverMock = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly LoginCommandHandler _handler;

    private const string DummyPasswordHash = "AQIDBAUGBwgJCgsMDQ4PEA==:AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=";

    public LoginCommandHandlerTests()
    {
        _handler = new LoginCommandHandler(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _tokenServiceMock.Object,
            _permissionResolverMock.Object,
            _timeProvider);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldRunConstantTimeDummyVerification_AndThrowInvalidCredentialsException()
    {
        // Arrange
        _userRepositoryMock.Setup(r => r.GetByEmailAsync("nonexistent@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var command = new LoginCommand("nonexistent@test.com", "Password123!");

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<InvalidCredentialsException>();
        ex.Which.Code.Should().Be("INVALID_CREDENTIALS");

        // Verify timing attack defense: dummy hash verification was executed
        _passwordHasherMock.Verify(p => p.VerifyPassword("Password123!", DummyPasswordHash), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserPasswordHashIsNull_ShouldRunDummyVerification_AndThrowInvalidCredentialsException()
    {
        // Arrange: Guest or social user without password
        var guestUser = User.CreateGuest();
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(guestUser.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(guestUser);

        var command = new LoginCommand(guestUser.Email, "Password123!");

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<InvalidCredentialsException>();
        ex.Which.Code.Should().Be("INVALID_CREDENTIALS");

        _passwordHasherMock.Verify(p => p.VerifyPassword("Password123!", DummyPasswordHash), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPasswordIncorrect_ShouldThrowInvalidCredentialsException()
    {
        // Arrange
        var user = User.CreateStandard("user@test.com", "valid_hash");
        _userRepositoryMock.Setup(r => r.GetByEmailAsync("user@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(p => p.VerifyPassword("WrongPassword123!", "valid_hash"))
            .Returns(false);

        var command = new LoginCommand("user@test.com", "WrongPassword123!");

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<InvalidCredentialsException>();
        ex.Which.Code.Should().Be("INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Handle_WhenCredentialsValid_ShouldAddRefreshToken_Save_AndReturnAuthResponse()
    {
        // Arrange
        var user = User.CreateStandard("user@test.com", "valid_hash");
        _userRepositoryMock.Setup(r => r.GetByEmailAsync("user@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(p => p.VerifyPassword("CorrectPassword123!", "valid_hash"))
            .Returns(true);
        _tokenServiceMock.Setup(t => t.GenerateRefreshToken())
            .Returns("raw_refresh_token");
        _tokenServiceMock.Setup(t => t.HashToken(It.IsAny<string>()))
            .Returns("hashed_refresh_token");
        _permissionResolverMock.Setup(p => p.ResolveEffectivePermissionsAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(([SystemRoles.Standard], ["catalog:read"]));
        _tokenServiceMock.Setup(t => t.GenerateTokensAsync(user, It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenResponse("access_jwt_token", "raw_refresh_token", 3600));

        var command = new LoginCommand("user@test.com", "CorrectPassword123!", "127.0.0.1");

        // Act
        var response = await _handler.Handle(command, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.UserId.Should().Be(user.Id);
        response.Email.Should().Be("user@test.com");
        response.AccessToken.Should().Be("access_jwt_token");
        response.RefreshToken.Should().Be("raw_refresh_token");
        user.RefreshTokens.Should().ContainSingle(rt => rt.TokenHash == "hashed_refresh_token");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
