using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ShuffleSeries.Shared.Core.Infrastructure.Configuration.Redis;
using ShuffleSeries.Shared.Core.Infrastructure.Services.Security;
using StackExchange.Redis;

namespace ShuffleSeries.Shared.Core.Tests.Infrastructure.Security;

public class RedisTokenBlacklistServiceTests
{
    private readonly Mock<IConnectionMultiplexer> _redisMock = new();
    private readonly Mock<IDatabase> _databaseMock = new();
    private readonly Mock<ILogger<RedisTokenBlacklistService>> _loggerMock = new();
    private readonly RedisOptions _options = new() { InstanceName = "test:" };
    private readonly RedisTokenBlacklistService _sut;

    public RedisTokenBlacklistServiceTests()
    {
        _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_databaseMock.Object);

        var optionsWrapper = Options.Create(_options);
        _sut = new RedisTokenBlacklistService(_redisMock.Object, optionsWrapper, _loggerMock.Object);
    }

    [Fact]
    public async Task BlacklistTokenAsync_WhenValidJtiAndPositiveTtl_ShouldWriteToRedisWithFormattedKey()
    {
        // Arrange
        var jti = Guid.NewGuid().ToString();
        var ttl = TimeSpan.FromMinutes(15);
        var expectedKey = $"test:blacklist:token:{jti}";

        _databaseMock.Setup(db => db.StringSetAsync(
                It.Is<RedisKey>(k => k == expectedKey),
                It.Is<RedisValue>(v => v == "logout"),
                It.Is<TimeSpan?>(t => t == ttl),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        await _sut.BlacklistTokenAsync(jti, ttl, "logout");

        // Assert
        _databaseMock.Invocations.Should().ContainSingle(i =>
            i.Method.Name == nameof(IDatabase.StringSetAsync) &&
            i.Arguments[0].ToString() == expectedKey &&
            i.Arguments[1].ToString() == "logout" &&
            i.Arguments[2].ToString() == $"EX {ttl.TotalSeconds}");
    }

    [Fact]
    public async Task BlacklistTokenAsync_WhenTtlIsZeroOrNegative_ShouldSkipRedisWrite()
    {
        // Arrange
        var jti = "already-expired-jti";

        // Act
        await _sut.BlacklistTokenAsync(jti, TimeSpan.Zero);
        await _sut.BlacklistTokenAsync(jti, TimeSpan.FromMinutes(-5));

        // Assert
        _databaseMock.Verify(db => db.StringSetAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<RedisValue>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<When>(),
            It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task BlacklistTokenAsync_WhenRedisThrows_ShouldCatchAndNotThrow()
    {
        // Arrange
        var jti = "faulty-redis-jti";
        _databaseMock.Setup(db => db.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Redis unreachable"));

        // Act & Assert
        var act = async () => await _sut.BlacklistTokenAsync(jti, TimeSpan.FromMinutes(5));
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task IsTokenBlacklistedAsync_WhenKeyExists_ShouldReturnTrue()
    {
        // Arrange
        var jti = Guid.NewGuid().ToString();
        var expectedKey = $"test:blacklist:token:{jti}";

        _databaseMock.Setup(db => db.KeyExistsAsync(
                It.Is<RedisKey>(k => k == expectedKey),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.IsTokenBlacklistedAsync(jti);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsTokenBlacklistedAsync_WhenKeyDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        var jti = Guid.NewGuid().ToString();
        var expectedKey = $"test:blacklist:token:{jti}";

        _databaseMock.Setup(db => db.KeyExistsAsync(
                It.Is<RedisKey>(k => k == expectedKey),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.IsTokenBlacklistedAsync(jti);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsTokenBlacklistedAsync_WhenRedisThrows_ShouldFailOpenAndReturnFalse()
    {
        // Arrange
        var jti = "faulty-check-jti";
        _databaseMock.Setup(db => db.KeyExistsAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisTimeoutException("Timeout", CommandStatus.WaitingToBeSent));

        // Act
        var result = await _sut.IsTokenBlacklistedAsync(jti);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task BlacklistUserTokensAsync_WhenValidUserId_ShouldStoreUnixTimestampWithTtl()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ttl = TimeSpan.FromHours(2);
        var expectedKey = $"test:blacklist:user:{userId:N}";

        _databaseMock.Setup(db => db.StringSetAsync(
                It.Is<RedisKey>(k => k == expectedKey),
                It.IsAny<RedisValue>(),
                It.Is<TimeSpan?>(t => t == ttl),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        await _sut.BlacklistUserTokensAsync(userId, ttl);

        // Assert
        _databaseMock.Invocations.Should().ContainSingle(i =>
            i.Method.Name == nameof(IDatabase.StringSetAsync) &&
            i.Arguments[0].ToString() == expectedKey &&
            i.Arguments[2].ToString() == $"EX {ttl.TotalSeconds}");
    }

    [Fact]
    public async Task IsUserBlacklistedAsync_WhenTokenIssuedBeforeRevocation_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedKey = $"test:blacklist:user:{userId:N}";
        var revokedAt = DateTimeOffset.UtcNow;
        var tokenIssuedAt = revokedAt.AddMinutes(-5).UtcDateTime; // Issued before revocation

        _databaseMock.Setup(db => db.StringGetAsync(
                It.Is<RedisKey>(k => k == expectedKey),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(revokedAt.ToUnixTimeSeconds().ToString());

        // Act
        var result = await _sut.IsUserBlacklistedAsync(userId, tokenIssuedAt);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsUserBlacklistedAsync_WhenTokenIssuedAfterRevocation_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedKey = $"test:blacklist:user:{userId:N}";
        var revokedAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        var tokenIssuedAt = DateTimeOffset.UtcNow.UtcDateTime; // Issued after revocation

        _databaseMock.Setup(db => db.StringGetAsync(
                It.Is<RedisKey>(k => k == expectedKey),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(revokedAt.ToUnixTimeSeconds().ToString());

        // Act
        var result = await _sut.IsUserBlacklistedAsync(userId, tokenIssuedAt);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsUserBlacklistedAsync_WhenNoRevocationRecord_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedKey = $"test:blacklist:user:{userId:N}";

        _databaseMock.Setup(db => db.StringGetAsync(
                It.Is<RedisKey>(k => k == expectedKey),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        // Act
        var result = await _sut.IsUserBlacklistedAsync(userId, DateTime.UtcNow);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsUserBlacklistedAsync_WhenRedisThrows_ShouldFailOpenAndReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _databaseMock.Setup(db => db.StringGetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisServerException("ERR server down"));

        // Act
        var result = await _sut.IsUserBlacklistedAsync(userId, DateTime.UtcNow);

        // Assert
        result.Should().BeFalse();
    }
}
