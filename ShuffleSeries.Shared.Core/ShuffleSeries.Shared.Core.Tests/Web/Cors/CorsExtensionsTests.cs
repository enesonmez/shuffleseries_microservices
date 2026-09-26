using AwesomeAssertions;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShuffleSeries.Shared.Core.Web.Cors;

namespace ShuffleSeries.Shared.Core.Tests.Web.Cors;

public class CorsExtensionsTests
{
    [Fact]
    public void AddSharedCors_WithoutAllowedOrigins_ShouldAllowAnyOrigin()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build(); // empty

        services.AddSharedCors(configuration);

        var sp = services.BuildServiceProvider();
        var corsOptions = sp.GetRequiredService<IOptions<CorsOptions>>().Value;

        var defaultPolicy = corsOptions.GetPolicy(CorsExtensions.DefaultCorsPolicyName);
        defaultPolicy.Should().NotBeNull();
        defaultPolicy!.AllowAnyOrigin.Should().BeTrue();
        defaultPolicy.AllowAnyMethod.Should().BeTrue();
        defaultPolicy.AllowAnyHeader.Should().BeTrue();
    }

    [Fact]
    public void AddSharedCors_WithAllowedOrigins_ShouldRestrictToOriginsAndSupportCredentials()
    {
        var services = new ServiceCollection();
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = "https://shuffleseries.com",
            ["Cors:AllowedOrigins:1"] = "https://app.shuffleseries.com"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        services.AddSharedCors(configuration);

        var sp = services.BuildServiceProvider();
        var corsOptions = sp.GetRequiredService<IOptions<CorsOptions>>().Value;

        var defaultPolicy = corsOptions.GetPolicy(CorsExtensions.DefaultCorsPolicyName);
        defaultPolicy.Should().NotBeNull();
        defaultPolicy!.AllowAnyOrigin.Should().BeFalse();
        defaultPolicy.Origins.Should().Contain(["https://shuffleseries.com", "https://app.shuffleseries.com"]);
        defaultPolicy.SupportsCredentials.Should().BeTrue();
    }
}
