using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShuffleSeries.Shared.Core.Web.ForwardedHeaders;

namespace ShuffleSeries.Shared.Core.Tests.Web.ForwardedHeaders;

public class ForwardedHeadersExtensionsTests
{
    [Fact]
    public void AddSharedForwardedHeaders_ShouldConfigureDefaults_AndKnownIPNetworks()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        services.AddSharedForwardedHeaders(configuration);
        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        // Assert
        options.ForwardLimit.Should().Be(1);
        options.ForwardedHeaders.Should().HaveFlag(Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor);
        options.ForwardedHeaders.Should().HaveFlag(Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto);
        options.KnownIPNetworks.Should().NotBeEmpty();
    }

    [Fact]
    public void AddSharedForwardedHeaders_ShouldBindKnownProxies_WhenConfiguredAsArray()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ForwardedHeaders:KnownProxies:0"] = "192.0.2.1",
                ["ForwardedHeaders:KnownProxies:1"] = "192.0.2.2"
            })
            .Build();

        // Act
        services.AddSharedForwardedHeaders(configuration);
        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        // Assert
        options.KnownProxies.Should().Contain(IPAddress.Parse("192.0.2.1"));
        options.KnownProxies.Should().Contain(IPAddress.Parse("192.0.2.2"));
    }

    [Fact]
    public void AddSharedForwardedHeaders_ShouldBindKnownProxies_WhenConfiguredAsCommaSeparatedString()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ForwardedHeaders:KnownProxies"] = "198.51.100.1, 198.51.100.2"
            })
            .Build();

        // Act
        services.AddSharedForwardedHeaders(configuration);
        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        // Assert
        options.KnownProxies.Should().Contain(IPAddress.Parse("198.51.100.1"));
        options.KnownProxies.Should().Contain(IPAddress.Parse("198.51.100.2"));
    }
}
