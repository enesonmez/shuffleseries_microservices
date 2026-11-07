using Moq;
using ShuffleSeries.Identity.Application.Common.Interfaces;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.Login;
using ShuffleSeries.Identity.Domain.Entities;
using ShuffleSeries.Identity.Domain.Exceptions;
using ShuffleSeries.Identity.Domain.Repositories;

namespace ShuffleSeries.Identity.Tests.Application.Features.Auth.Commands.Login;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IAuthResponseService> _authResponseServiceMock = new();
    private readonly LoginCommandHandler _handler;

    private const string DummyPasswordHash = "AQIDBAUGBwgJCgsMDQ4PEA==:AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=";

    public LoginCommandHandlerTests()
    {
        _handler = new LoginCommandHandler(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _authResponseServiceMock.Object);
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
    public async Task Handle_WhenCredentialsValid_ShouldCallAuthResponseService_AndReturnAuthResponse()
    {
        // Arrange
        var user = User.CreateStandard("user@test.com", "valid_hash");
        _userRepositoryMock.Setup(r => r.GetByEmailAsync("user@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(p => p.VerifyPassword("CorrectPassword123!", "valid_hash"))
            .Returns(true);

        var expectedResponse = new AuthResponse(
            user.Id,
            user.Email,
            user.IsGuest,
            [SystemRoles.Standard],
            [SystemPermissions.Catalog.MoviesRead],
            "access_jwt_token",
            "raw_refresh_token",
            3600);

        _authResponseServiceMock.Setup(a => a.CreateAuthResponseAsync(user, "127.0.0.1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var command = new LoginCommand("user@test.com", "CorrectPassword123!", "127.0.0.1");

        // Act
        var response = await _handler.Handle(command, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.UserId.Should().Be(user.Id);
        response.Email.Should().Be("user@test.com");
        response.AccessToken.Should().Be("access_jwt_token");
        response.RefreshToken.Should().Be("raw_refresh_token");
        _authResponseServiceMock.Verify(a => a.CreateAuthResponseAsync(user, "127.0.0.1", It.IsAny<CancellationToken>()), Times.Once);
    }
}
