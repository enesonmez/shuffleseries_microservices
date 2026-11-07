using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using ShuffleSeries.Identity.Domain.Entities;
using ShuffleSeries.Identity.Infrastructure.Services;

namespace ShuffleSeries.Identity.Tests.Infrastructure.Services;

public class JwtTokenServiceTests
{
    private const string TestSecret = "SuperSecretKeyForTestingJwtTokenGeneration1234567890!";
    private const string TestIssuer = "ShuffleSeries.Identity";
    private const string TestAudience = "ShuffleSeries.Clients";

    private readonly IConfiguration _configuration;
    private readonly JwtTokenService _sut;

    public JwtTokenServiceTests()
    {
        var configDict = new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = TestSecret,
            ["Jwt:Issuer"] = TestIssuer,
            ["Jwt:Audience"] = TestAudience,
            ["Jwt:ExpirationMinutes"] = "60"
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        _sut = new JwtTokenService(_configuration, TimeProvider.System);
    }

    [Fact]
    public async Task GenerateTokensAsync_ShouldProduceValidSignedJwt_WithCorrectClaims()
    {
        // Arrange
        var user = User.CreateStandard("test@example.com", "hashed_pw");
        var roles = new[] { "standard", "vip" };
        var permissions = new[] { "catalog:movies:read", "shuffle:basic" };

        // Act
        var result = await _sut.GenerateTokensAsync(user, roles, permissions);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.ExpiresIn.Should().Be(3600);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(result.AccessToken);

        jwt.Issuer.Should().Be(TestIssuer);
        jwt.Audiences.Should().Contain(TestAudience);
        jwt.Subject.Should().Be(user.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "test@example.com");
        jwt.Claims.Should().Contain(c => c.Type == "is_guest" && c.Value == "false");
        jwt.Claims.Should().Contain(c => c.Type == "role" && c.Value == "standard");
        jwt.Claims.Should().Contain(c => c.Type == "role" && c.Value == "vip");
        jwt.Claims.Should().Contain(c => c.Type == "permission" && c.Value == "catalog:movies:read");
        jwt.Claims.Should().Contain(c => c.Type == "permission" && c.Value == "shuffle:basic");
    }

    [Fact]
    public void AccessTokenLifetime_ShouldMatchConfiguredExpirationMinutes()
    {
        _sut.AccessTokenLifetime.Should().Be(TimeSpan.FromMinutes(60));

        var customConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = TestSecret,
                ["Jwt:ExpirationMinutes"] = "120"
            })
            .Build();

        var customSut = new JwtTokenService(customConfig, TimeProvider.System);
        customSut.AccessTokenLifetime.Should().Be(TimeSpan.FromMinutes(120));
    }

    [Fact]
    public void RefreshTokenLifetime_ShouldDefaultTo14Days_AndMatchCustomConfiguration()
    {
        // Default when not specified in _configuration
        _sut.RefreshTokenLifetime.Should().Be(TimeSpan.FromDays(14));

        var customConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = TestSecret,
                ["Jwt:RefreshTokenExpirationDays"] = "7"
            })
            .Build();

        var customSut = new JwtTokenService(customConfig, TimeProvider.System);
        customSut.RefreshTokenLifetime.Should().Be(TimeSpan.FromDays(7));
    }

    [Fact]
    public void GuestRefreshTokenLifetime_ShouldDefaultTo30Days_AndMatchCustomConfiguration()
    {
        // Default when not specified in _configuration
        _sut.GuestRefreshTokenLifetime.Should().Be(TimeSpan.FromDays(30));

        var customConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = TestSecret,
                ["Jwt:GuestRefreshTokenExpirationDays"] = "45"
            })
            .Build();

        var customSut = new JwtTokenService(customConfig, TimeProvider.System);
        customSut.GuestRefreshTokenLifetime.Should().Be(TimeSpan.FromDays(45));
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnUrlSafeBase64String_WithHighEntropy()
    {
        // Act
        var token1 = _sut.GenerateRefreshToken();
        var token2 = _sut.GenerateRefreshToken();

        // Assert
        token1.Should().NotBeNullOrWhiteSpace();
        token2.Should().NotBeNullOrWhiteSpace();
        token1.Should().NotBe(token2);
        token1.Should().NotContain("+").And.NotContain("/").And.NotContain("=");
    }

    [Fact]
    public void HashToken_ShouldProduceDeterministicSha256HexString()
    {
        // Arrange
        const string raw = "refresh_token_sample_12345";

        // Act
        var hash1 = _sut.HashToken(raw);
        var hash2 = _sut.HashToken(raw);

        // Assert
        hash1.Should().Be(hash2);
        hash1.Length.Should().Be(64);
        hash1.Should().MatchRegex("^[a-f0-9]{64}$");
    }

    [Fact]
    public async Task ExtractTokenInfo_ShouldExtractJtiAndExpiration_WhenTokenValid()
    {
        // Arrange
        var user = User.CreateStandard("extract@example.com", "pw_hash");
        var token = await _sut.GenerateTokensAsync(user, ["standard"], []);

        // Act
        var (jwtId, expiresAt) = _sut.ExtractTokenInfo(token.AccessToken, user.Id);

        // Assert
        jwtId.Should().NotBeNullOrWhiteSpace();
        expiresAt.Should().NotBeNull();
        expiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task ExtractTokenInfo_ShouldReturnNull_WhenExpectedUserIdMismatches()
    {
        // Arrange
        var user = User.CreateStandard("user1@example.com", "pw_hash");
        var token = await _sut.GenerateTokensAsync(user, ["standard"], []);
        var otherUserId = Guid.NewGuid();

        // Act
        var (jwtId, expiresAt) = _sut.ExtractTokenInfo(token.AccessToken, otherUserId);

        // Assert
        jwtId.Should().BeNull();
        expiresAt.Should().BeNull();
    }

    [Fact]
    public void ExtractTokenInfo_ShouldReturnNull_WhenTokenIsTamperedOrInvalid()
    {
        // Act
        var (jwtId, expiresAt) = _sut.ExtractTokenInfo("invalid.jwt.token", Guid.NewGuid());

        // Assert
        jwtId.Should().BeNull();
        expiresAt.Should().BeNull();
    }
}
