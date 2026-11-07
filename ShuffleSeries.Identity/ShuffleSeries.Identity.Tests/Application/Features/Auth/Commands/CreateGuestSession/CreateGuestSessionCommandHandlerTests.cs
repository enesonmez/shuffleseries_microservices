using Moq;
using ShuffleSeries.Identity.Application.Common.Interfaces;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.CreateGuestSession;
using ShuffleSeries.Identity.Domain.Entities;
using ShuffleSeries.Identity.Domain.Repositories;

namespace ShuffleSeries.Identity.Tests.Application.Features.Auth.Commands.CreateGuestSession;

public class CreateGuestSessionCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IRoleRepository> _roleRepositoryMock = new();
    private readonly Mock<IAuthResponseService> _authResponseServiceMock = new();
    private readonly CreateGuestSessionCommandHandler _handler;

    public CreateGuestSessionCommandHandlerTests()
    {
        _handler = new CreateGuestSessionCommandHandler(
            _userRepositoryMock.Object,
            _roleRepositoryMock.Object,
            _authResponseServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenValidRequest_ShouldCreateGuestUser_AssignGuestRole_AndReturnAuthResponse()
    {
        // Arrange
        var guestRole = Role.Create(SystemRoles.Guest, "Guest");
        _roleRepositoryMock.Setup(r => r.GetByNameAsync(SystemRoles.Guest, It.IsAny<CancellationToken>()))
            .ReturnsAsync(guestRole);

        var expectedResponse = new AuthResponse(
            Guid.NewGuid(),
            "guest@shuffleseries.internal",
            true,
            [SystemRoles.Guest],
            [SystemPermissions.Catalog.MoviesRead],
            "guest_jwt_token",
            "raw_guest_refresh_token",
            3600);

        _authResponseServiceMock.Setup(a => a.CreateAuthResponseAsync(It.IsAny<User>(), "192.168.1.1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var command = new CreateGuestSessionCommand("192.168.1.1");

        // Act
        var response = await _handler.Handle(command, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.IsGuest.Should().BeTrue();
        response.Roles.Should().Contain(SystemRoles.Guest);
        response.AccessToken.Should().Be("guest_jwt_token");
        response.RefreshToken.Should().Be("raw_guest_refresh_token");
        _userRepositoryMock.Verify(r => r.Add(It.Is<User>(u => u.IsGuest)), Times.Once);
        _authResponseServiceMock.Verify(a => a.CreateAuthResponseAsync(It.IsAny<User>(), "192.168.1.1", It.IsAny<CancellationToken>()), Times.Once);
    }
}
