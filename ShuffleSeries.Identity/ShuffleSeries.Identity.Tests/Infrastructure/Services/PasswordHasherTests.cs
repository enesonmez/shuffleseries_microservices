using ShuffleSeries.Identity.Infrastructure.Services;

namespace ShuffleSeries.Identity.Tests.Infrastructure.Services;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void HashPassword_ShouldReturnSaltAndHashFormat()
    {
        // Act
        var hash = _hasher.HashPassword("MySecretP@ssw0rd");

        // Assert
        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().Contain(":");
        hash.Split(':').Should().HaveCount(2);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        const string password = "ValidPassword123!";
        var hash = _hasher.HashPassword(password);

        // Act
        var isValid = _hasher.VerifyPassword(password, hash);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithWrongPassword_ShouldReturnFalse()
    {
        // Arrange
        var hash = _hasher.HashPassword("CorrectPassword");

        // Act
        var isValid = _hasher.VerifyPassword("WrongPassword", hash);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithKnownPbkdf2HmacSha512Vector_ShouldReturnTrue()
    {
        // Arrange — RFC 2898 PBKDF2-HMAC-SHA512 (100,000 iterations, 16-byte salt [1..16], 32-byte output)
        const string knownHash = "AQIDBAUGBwgJCgsMDQ4PEA==:ti06sjaXeqguZRogVkA/CtarTvJNHrjB3YWOWwR+1C0=";

        // Act
        var isValid = _hasher.VerifyPassword("TestVectorPassword123!", knownHash);

        // Assert
        isValid.Should().BeTrue();
    }
}
