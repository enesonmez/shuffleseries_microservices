using Moq;
using ShuffleSeries.Identity.Application.Features.Auth.Queries.GetCurrentUser;
using ShuffleSeries.Identity.Application.Interfaces;
using ShuffleSeries.Identity.Domain.Entities;
using ShuffleSeries.Identity.Domain.Exceptions;
using ShuffleSeries.Identity.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Constants;

namespace ShuffleSeries.Identity.Tests.Application;

public class GetCurrentUserQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IPermissionResolver> _permissionResolverMock = new();
    private readonly GetCurrentUserQueryHandler _handler;

    public GetCurrentUserQueryHandlerTests()
    {
        _handler = new GetCurrentUserQueryHandler(
            _userRepositoryMock.Object,
            _permissionResolverMock.Object);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldThrowUserNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var query = new GetCurrentUserQuery(userId);

        // Act
        var act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenUserExists_ShouldReturnCurrentUserResponse()
    {
        // Arrange
        var user = User.CreateStandard("user@test.com", "hash");
        _userRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _permissionResolverMock.Setup(p => p.ResolveEffectivePermissionsAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(([SystemRoles.Standard], ["catalog:read"]));

        var query = new GetCurrentUserQuery(user.Id);

        // Act
        var response = await _handler.Handle(query, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.UserId.Should().Be(user.Id);
        response.Email.Should().Be("user@test.com");
        response.Roles.Should().Contain(SystemRoles.Standard);
        response.Permissions.Should().Contain("catalog:read");
        response.IsGuest.Should().BeFalse();
        response.Status.Should().Be("Active");
    }
}
