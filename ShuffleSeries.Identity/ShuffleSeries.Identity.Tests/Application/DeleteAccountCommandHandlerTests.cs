using Moq;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.DeleteAccount;
using ShuffleSeries.Identity.Domain.Entities;
using ShuffleSeries.Identity.Domain.Exceptions;
using ShuffleSeries.Identity.Domain.Repositories;
using ShuffleSeries.Shared.Core.Application.Security;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Identity.Tests.Application;

public class DeleteAccountCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ITokenBlacklistService> _blacklistServiceMock = new();
    private readonly DeleteAccountCommandHandler _handler;

    public DeleteAccountCommandHandlerTests()
    {
        _handler = new DeleteAccountCommandHandler(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _blacklistServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldThrowUserNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var act = async () => await _handler.Handle(new DeleteAccountCommand(userId), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenUserExists_ShouldDeleteAccount_BlacklistUserTokens_AndSave()
    {
        // Arrange
        var user = User.CreateStandard("user@test.com", "hash");
        user.AddRefreshToken("hash1", DateTime.UtcNow.AddDays(7));

        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(new DeleteAccountCommand(user.Id), CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        user.IsDeleted.Should().BeTrue();
        user.RefreshTokens.All(r => r.IsRevoked).Should().BeTrue();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _blacklistServiceMock.Verify(b => b.BlacklistUserTokensAsync(user.Id, TimeSpan.FromHours(1), It.IsAny<CancellationToken>()), Times.Once);
    }
}
