using Moq;
using ShuffleSeries.Identity.Application.Common.Interfaces;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.Register;
using ShuffleSeries.Identity.Domain.Entities;
using ShuffleSeries.Identity.Domain.Exceptions;
using ShuffleSeries.Identity.Domain.Repositories;

namespace ShuffleSeries.Identity.Tests.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IRoleRepository> _roleRepositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IAuthResponseService> _authResponseServiceMock = new();
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _handler = new RegisterCommandHandler(
            _userRepositoryMock.Object,
            _roleRepositoryMock.Object,
            _passwordHasherMock.Object,
            _authResponseServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ShouldThrowEmailAlreadyInUseException()
    {
        // Arrange
        var command = new RegisterCommand("existing@test.com", "Password123!");
        _userRepositoryMock.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<EmailAlreadyInUseException>();
        ex.Which.Code.Should().Be("USER_ALREADY_EXISTS");
    }

    [Fact]
    public async Task Handle_WhenValidRequest_ShouldCreateUser_AndReturnAuthResponse()
    {
        // Arrange
        var command = new RegisterCommand("new@test.com", "Password123!");
        var role = Role.Create(SystemRoles.Standard, "Standard");

        _userRepositoryMock.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _roleRepositoryMock.Setup(r => r.GetDefaultRoleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);
        _passwordHasherMock.Setup(p => p.HashPassword(It.IsAny<string>()))
            .Returns("hashed_pwd");

        var expectedResponse = new AuthResponse(
            Guid.NewGuid(),
            "new@test.com",
            false,
            [SystemRoles.Standard],
            [SystemPermissions.Catalog.MoviesRead],
            "access_jwt",
            "raw_refresh_token",
            3600);

        _authResponseServiceMock.Setup(a => a.CreateAuthResponseAsync(It.IsAny<User>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var response = await _handler.Handle(command, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.Email.Should().Be("new@test.com");
        response.Roles.Should().Contain(SystemRoles.Standard);
        response.Permissions.Should().Contain(SystemPermissions.Catalog.MoviesRead);
        response.AccessToken.Should().Be("access_jwt");
        _userRepositoryMock.Verify(r => r.Add(It.IsAny<User>()), Times.Once);
        _authResponseServiceMock.Verify(a => a.CreateAuthResponseAsync(It.IsAny<User>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
