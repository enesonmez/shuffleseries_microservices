using Moq;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.ConvertGuest;
using ShuffleSeries.Identity.Application.Interfaces;
using ShuffleSeries.Identity.Application.Models;
using ShuffleSeries.Identity.Domain.Entities;
using ShuffleSeries.Identity.Domain.Enums;
using ShuffleSeries.Identity.Domain.Exceptions;
using ShuffleSeries.Identity.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Constants;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Identity.Tests.Application;

public class ConvertGuestCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IRoleRepository> _roleRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<IPermissionResolver> _permissionResolverMock = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly ConvertGuestCommandHandler _handler;

    public ConvertGuestCommandHandlerTests()
    {
        _handler = new ConvertGuestCommandHandler(
            _userRepositoryMock.Object,
            _roleRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _tokenServiceMock.Object,
            _permissionResolverMock.Object,
            _timeProvider);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldThrowUserNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var command = new ConvertGuestCommand(userId, "converted@test.com", "Password123!");

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenUserIsNotGuest_ShouldThrowUserAlreadyRegisteredException()
    {
        // Arrange
        var user = User.CreateStandard("standard@test.com", "hash");
        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var command = new ConvertGuestCommand(user.Id, "converted@test.com", "Password123!");

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<UserAlreadyRegisteredException>();
        ex.Which.Code.Should().Be("USER_ALREADY_REGISTERED");
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyInUse_ShouldThrowEmailAlreadyInUseException()
    {
        // Arrange
        var guestUser = User.CreateGuest();
        _userRepositoryMock.Setup(r => r.GetByIdAsync(guestUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(guestUser);
        _userRepositoryMock.Setup(r => r.ExistsByEmailAsync("existing@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new ConvertGuestCommand(guestUser.Id, "existing@test.com", "Password123!");

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<EmailAlreadyInUseException>();
        ex.Which.Code.Should().Be("USER_ALREADY_EXISTS");
    }

    [Fact]
    public async Task Handle_WhenValidGuest_ShouldConvertUser_AssignStandardRole_AndReturnAuthResponse()
    {
        // Arrange
        var guestUser = User.CreateGuest();
        var guestRole = Role.Create(SystemRoles.Guest, "Guest");
        guestUser.AssignRole(guestRole.Id);

        var standardRole = Role.Create(SystemRoles.Standard, "Standard");

        _userRepositoryMock.Setup(r => r.GetByIdAsync(guestUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(guestUser);
        _userRepositoryMock.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _roleRepositoryMock.Setup(r => r.GetByNameAsync(SystemRoles.Guest, It.IsAny<CancellationToken>()))
            .ReturnsAsync(guestRole);
        _roleRepositoryMock.Setup(r => r.GetDefaultRoleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(standardRole);
        _passwordHasherMock.Setup(p => p.HashPassword(It.IsAny<string>()))
            .Returns("new_hashed_pwd");
        _tokenServiceMock.Setup(t => t.GenerateRefreshToken())
            .Returns("raw_refresh_token");
        _tokenServiceMock.Setup(t => t.HashToken(It.IsAny<string>()))
            .Returns("hashed_refresh_token");
        _tokenServiceMock.Setup(t => t.GenerateTokensAsync(It.IsAny<User>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenResponse("access_jwt", "raw_refresh_token", 3600));
        _permissionResolverMock.Setup(p => p.ResolveEffectivePermissionsAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(([SystemRoles.Standard], ["catalog:read"]));

        var command = new ConvertGuestCommand(guestUser.Id, "converted@test.com", "Password123!", "127.0.0.1");

        // Act
        var response = await _handler.Handle(command, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.UserId.Should().Be(guestUser.Id);
        response.Email.Should().Be("converted@test.com");
        response.IsGuest.Should().BeFalse();
        guestUser.IsGuest.Should().BeFalse();
        guestUser.Status.Should().Be(UserStatus.Active);
        guestUser.Email.Should().Be("converted@test.com");
        guestUser.UserRoles.Should().NotContain(ur => ur.RoleId == guestRole.Id);
        guestUser.UserRoles.Should().Contain(ur => ur.RoleId == standardRole.Id);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
