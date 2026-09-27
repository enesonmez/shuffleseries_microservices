using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;
using Serilog;
using ShuffleSeries.Shared.Core.Web.Logging;

namespace ShuffleSeries.Shared.Core.Tests.Web.Logging;

public class SerilogLoggingExtensibilityTests
{
    private readonly Mock<IHostEnvironment> _mockEnvironment = new();

    public SerilogLoggingExtensibilityTests()
    {
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Testing");
    }

    [Fact]
    public void SerilogLoggingBuilder_WhenCustomSinkAdded_ShouldRegisterAndExecute()
    {
        // Arrange
        var builder = new SerilogLoggingBuilder();
        var customConfiguratorExecuted = false;

        builder.AddCustomConfiguration((loggerConfig, config, env) =>
        {
            customConfiguratorExecuted = true;
        });

        var loggerConfig = new LoggerConfiguration();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        foreach (var configurator in builder.SinkConfigurators)
        {
            configurator.Configure(loggerConfig, configuration, _mockEnvironment.Object);
        }

        // Assert
        customConfiguratorExecuted.Should().BeTrue();
    }

    [Fact]
    public void ConsoleSinkConfigurator_WhenDisabled_ShouldNotThrow()
    {
        // Arrange
        var configurator = new ConsoleSinkConfigurator();
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Logging:Sinks:Console:Enabled", "false" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var loggerConfig = new LoggerConfiguration();

        // Act
        Action act = () => configurator.Configure(loggerConfig, configuration, _mockEnvironment.Object);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void FileSinkConfigurator_WhenDisabled_ShouldNotThrow()
    {
        // Arrange
        var configurator = new FileSinkConfigurator();
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Logging:Sinks:File:Enabled", "false" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var loggerConfig = new LoggerConfiguration();

        // Act
        Action act = () => configurator.Configure(loggerConfig, configuration, _mockEnvironment.Object);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void PostgreSqlSinkConfigurator_WhenNoConnectionString_ShouldGracefullyReturn()
    {
        // Arrange
        var configurator = new PostgreSqlSinkConfigurator();
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Logging:Sinks:PostgreSql:Enabled", "true" },
            { "Logging:Sinks:PostgreSql:ConnectionString", "" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var loggerConfig = new LoggerConfiguration();

        // Act
        Action act = () => configurator.Configure(loggerConfig, configuration, _mockEnvironment.Object);

        // Assert
        act.Should().NotThrow();
    }
}
