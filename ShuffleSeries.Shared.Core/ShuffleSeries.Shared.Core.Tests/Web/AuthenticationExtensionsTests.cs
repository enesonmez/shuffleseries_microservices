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
        authorizationOptions.GetPolicy("CanReadCatalog").Should().NotBeNull();

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
}
