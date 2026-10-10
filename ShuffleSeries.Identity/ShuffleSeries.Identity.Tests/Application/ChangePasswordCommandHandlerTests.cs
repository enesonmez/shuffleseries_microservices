using Moq;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.ChangePassword;
using ShuffleSeries.Identity.Application.Interfaces;
using ShuffleSeries.Identity.Domain.Entities;
using ShuffleSeries.Identity.Domain.Exceptions;
using ShuffleSeries.Identity.Domain.Repositories;
using ShuffleSeries.Shared.Core.Application.Security;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Identity.Tests.Application;

public class ChangePasswordCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<ITokenBlacklistService> _blacklistServiceMock = new();
    private readonly ChangePasswordCommandHandler _handler;

    public ChangePasswordCommandHandlerTests()
    {
        _handler = new ChangePasswordCommandHandler(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _blacklistServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldThrowUserNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var command = new ChangePasswordCommand(userId, "OldPassword123!", "NewPassword123!");

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenPasswordHashIsNull_ShouldThrowInvalidCurrentPasswordException()
    {
        // Arrange
        var user = User.CreateGuest();
        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var command = new ChangePasswordCommand(user.Id, "OldPassword123!", "NewPassword123!");

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<InvalidCurrentPasswordException>();
        ex.Which.Code.Should().Be("INVALID_CURRENT_PASSWORD");
    }

    [Fact]
    public async Task Handle_WhenCurrentPasswordIncorrect_ShouldThrowInvalidCurrentPasswordException()
    {
        // Arrange
        var user = User.CreateStandard("user@test.com", "existing_hash");
        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(p => p.VerifyPassword("WrongPassword123!", "existing_hash"))
            .Returns(false);

        var command = new ChangePasswordCommand(user.Id, "WrongPassword123!", "NewPassword123!");

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<InvalidCurrentPasswordException>();
        ex.Which.Code.Should().Be("INVALID_CURRENT_PASSWORD");
    }

    [Fact]
    public async Task Handle_WhenCurrentPasswordCorrect_ShouldUpdatePassword_BlacklistTokens_AndReturnTrue()
    {
        // Arrange
        var user = User.CreateStandard("user@test.com", "old_hash");
        user.AddRefreshToken("token_hash_1", DateTime.UtcNow.AddDays(7));
        var initialStamp = user.SecurityStamp;

        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(p => p.VerifyPassword("OldPassword123!", "old_hash"))
            .Returns(true);
        _passwordHasherMock.Setup(p => p.HashPassword("NewPassword123!"))
            .Returns("new_hash");

        var command = new ChangePasswordCommand(user.Id, "OldPassword123!", "NewPassword123!");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        user.PasswordHash.Should().Be("new_hash");
        user.SecurityStamp.Should().NotBe(initialStamp);
        user.RefreshTokens.All(r => r.IsRevoked).Should().BeTrue();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _blacklistServiceMock.Verify(b => b.BlacklistUserTokensAsync(user.Id, TimeSpan.FromHours(1), It.IsAny<CancellationToken>()), Times.Once);
    }
}
