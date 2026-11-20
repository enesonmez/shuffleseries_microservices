using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShuffleSeries.Shared.Core.Web.Authentication;

namespace ShuffleSeries.Shared.Core.Tests.Web;

public class AuthenticationExtensionsTests
{
    [Fact]
    public void AddSharedJwtAuthentication_WhenConfigurationIsValid_ShouldRegisterAuthenticationAndAuthorization()
    {
        // Arrange
        var services = new ServiceCollection();
        var inMemorySettings = new Dictionary<string, string?> {
            {"Jwt:Secret", "this_is_a_very_secret_key_that_is_long_enough"},
            {"Jwt:Issuer", "TestIssuer"},
            {"Jwt:Audience", "TestAudience"}
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        services.AddSharedJwtAuthentication(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var authenticationOptions = serviceProvider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
        authenticationOptions.DefaultScheme.Should().Be("Bearer");

        var authorizationOptions = serviceProvider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
        authorizationOptions.GetPolicy("RequirePremiumRole").Should().NotBeNull();
        authorizationOptions.GetPolicy("CanReadCatalogMovies").Should().NotBeNull();
        authorizationOptions.GetPolicy("CanCreateCatalogMovies").Should().NotBeNull();
        authorizationOptions.GetPolicy("CanUpdateCatalogMovies").Should().NotBeNull();
        authorizationOptions.GetPolicy("CanDeleteCatalogMovies").Should().NotBeNull();
        authorizationOptions.GetPolicy("CanReadCatalogSeries").Should().NotBeNull();
        authorizationOptions.GetPolicy("CanReadCatalogEpisodes").Should().NotBeNull();
        authorizationOptions.GetPolicy("CanReadCatalogPlatforms").Should().NotBeNull();
        authorizationOptions.GetPolicy("CanReadCatalogMoods").Should().NotBeNull();
        authorizationOptions.GetPolicy("CanReadCatalogGenres").Should().NotBeNull();
        authorizationOptions.GetPolicy("CanManageCatalogAssociations").Should().NotBeNull();
        authorizationOptions.GetPolicy("CanReadAdminMedia").Should().NotBeNull();

        var resultHandler = serviceProvider.GetService<IAuthorizationMiddlewareResultHandler>();
        resultHandler.Should().NotBeNull();
        resultHandler.Should().BeOfType<ShuffleSeries.Shared.Core.Web.Authorization.ProblemDetailsAuthorizationMiddlewareResultHandler>();
    }

    [Fact]
    public void AddSharedJwtAuthentication_WhenConfigurationIsMissing_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().Build(); // Boş config

        // Act
        Action act = () => services.AddSharedJwtAuthentication(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*Jwt:Secret*");
    }

    [Fact]
    public void AddSharedJwtAuthentication_ShouldEnforceZeroClockSkew_AndRegisterSecurityEvents()
    {
        // Arrange
        var services = new ServiceCollection();
        var inMemorySettings = new Dictionary<string, string?> {
            {"Jwt:Secret", "this_is_a_very_secret_key_that_is_long_enough"},
            {"Jwt:Issuer", "TestIssuer"},
            {"Jwt:Audience", "TestAudience"}
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        services.AddSharedJwtAuthentication(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var jwtOptions = serviceProvider.GetRequiredService<IOptionsMonitor<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>>()
            .Get(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme);

        jwtOptions.TokenValidationParameters.ClockSkew.Should().Be(TimeSpan.Zero);
        jwtOptions.TokenValidationParameters.ValidateIssuerSigningKey.Should().BeTrue();
        jwtOptions.TokenValidationParameters.ValidateIssuer.Should().BeTrue();
        jwtOptions.TokenValidationParameters.ValidateAudience.Should().BeTrue();
        jwtOptions.TokenValidationParameters.ValidateLifetime.Should().BeTrue();
        jwtOptions.Events.Should().NotBeNull();
        jwtOptions.Events.OnTokenValidated.Should().NotBeNull();
        jwtOptions.Events.OnChallenge.Should().NotBeNull();
    }

    [Fact]
    public void AddSharedJwtAuthentication_WhenValidateTokenBlacklistIsFalse_ShouldRegisterNoOpTokenValidated()
    {
        // Arrange
        var services = new ServiceCollection();
        var inMemorySettings = new Dictionary<string, string?> {
            {"Jwt:Secret", "this_is_a_very_secret_key_that_is_long_enough"},
            {"Jwt:Issuer", "TestIssuer"},
            {"Jwt:Audience", "TestAudience"}
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        services.AddSharedJwtAuthentication(configuration, validateTokenBlacklist: false);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var jwtOptions = serviceProvider.GetRequiredService<IOptionsMonitor<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>>()
            .Get(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme);

        jwtOptions.Events.Should().NotBeNull();
        jwtOptions.Events.OnTokenValidated.Should().NotBeNull();
    }

    [Theory]
    [InlineData("Production", false)]
    [InlineData("Development", true)]
    public async Task HandleJwtChallengeAsync_ShouldIncludeErrorDescriptionOnlyInDevelopment(string envName, bool shouldContainDescription)
    {
        // Arrange
        var services = new ServiceCollection();
        var inMemorySettings = new Dictionary<string, string?> {
            {"Jwt:Secret", "this_is_a_very_secret_key_that_is_long_enough"},
            {"Jwt:Issuer", "TestIssuer"},
            {"Jwt:Audience", "TestAudience"}
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var mockEnv = new Moq.Mock<Microsoft.Extensions.Hosting.IHostEnvironment>();
        mockEnv.Setup(e => e.EnvironmentName).Returns(envName);
        services.AddSingleton(mockEnv.Object);
        services.AddSharedJwtAuthentication(configuration);

        var serviceProvider = services.BuildServiceProvider();
        var jwtOptions = serviceProvider.GetRequiredService<IOptionsMonitor<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>>()
            .Get(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme);

        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        var stream = new MemoryStream();
        httpContext.Response.Body = stream;

        var scheme = new Microsoft.AspNetCore.Authentication.AuthenticationScheme("Bearer", "Bearer", typeof(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerHandler));
        var challengeContext = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerChallengeContext(
            httpContext,
            scheme,
            jwtOptions,
            new Microsoft.AspNetCore.Authentication.AuthenticationProperties())
        {
            Error = "invalid_token",
            ErrorDescription = "IDX10214: Audience validation failed."
        };

        // Act
        await jwtOptions.Events.OnChallenge(challengeContext);

        // Assert
        stream.Seek(0, SeekOrigin.Begin);
        var problemDetails = await System.Text.Json.JsonSerializer.DeserializeAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>(stream);
        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be(401);

        if (shouldContainDescription)
        {
            problemDetails.Extensions.Should().ContainKey("errorDescription");
            problemDetails.Extensions["errorDescription"]?.ToString().Should().Contain("IDX10214");
        }
        else
        {
            problemDetails.Extensions.Should().NotContainKey("errorDescription");
        }
    }
}
