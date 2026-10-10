using Moq;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.CreateGuestSession;
using ShuffleSeries.Identity.Application.Interfaces;
using ShuffleSeries.Identity.Application.Models;
using ShuffleSeries.Identity.Domain.Entities;
using ShuffleSeries.Identity.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Constants;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Identity.Tests.Application;

public class CreateGuestSessionCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IRoleRepository> _roleRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<IPermissionResolver> _permissionResolverMock = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly CreateGuestSessionCommandHandler _handler;

    public CreateGuestSessionCommandHandlerTests()
    {
        _handler = new CreateGuestSessionCommandHandler(
            _userRepositoryMock.Object,
            _roleRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _tokenServiceMock.Object,
            _permissionResolverMock.Object,
            _timeProvider);
    }

    [Fact]
    public async Task Handle_WhenValidRequest_ShouldCreateGuestUser_AssignGuestRole_AndReturnAuthResponse()
    {
        // Arrange
        var guestRole = Role.Create(SystemRoles.Guest, "Guest");
        _roleRepositoryMock.Setup(r => r.GetByNameAsync(SystemRoles.Guest, It.IsAny<CancellationToken>()))
            .ReturnsAsync(guestRole);
        _tokenServiceMock.Setup(t => t.GenerateRefreshToken())
            .Returns("raw_guest_refresh_token");
        _tokenServiceMock.Setup(t => t.HashToken(It.IsAny<string>()))
            .Returns("hashed_guest_refresh_token");
        _permissionResolverMock.Setup(p => p.ResolveEffectivePermissionsAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(([SystemRoles.Guest], ["catalog:read"]));
        _tokenServiceMock.Setup(t => t.GenerateTokensAsync(It.IsAny<User>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenResponse("guest_jwt_token", "raw_guest_refresh_token", 3600));

        var command = new CreateGuestSessionCommand("192.168.1.1");

        // Act
        var response = await _handler.Handle(command, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.IsGuest.Should().BeTrue();
        response.Roles.Should().Contain(SystemRoles.Guest);
        response.AccessToken.Should().Be("guest_jwt_token");
        response.RefreshToken.Should().Be("raw_guest_refresh_token");
        _userRepositoryMock.Verify(r => r.Add(It.Is<User>(u => u.IsGuest && u.RefreshTokens.Any(rt => rt.TokenHash == "hashed_guest_refresh_token"))), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
