using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShuffleSeries.Catalog.Application;
using Wolverine;

namespace ShuffleSeries.Catalog.Tests.Application.Messaging;

public record PingCatalogCommand(string Echo);
public record SlowCatalogCommand(int DelayMs);
public record FailingCatalogCommand;

public class TestCatalogPipelineHandler
{
    public string Handle(PingCatalogCommand cmd) => $"Pong: {cmd.Echo}";

    public async Task<string> Handle(SlowCatalogCommand cmd)
    {
        await Task.Delay(cmd.DelayMs);
        return "Done";
    }

    public void Handle(FailingCatalogCommand cmd)
    {
        throw new InvalidOperationException("Simulated Catalog Failure");
    }
}

public sealed class TestLogSink : ILoggerProvider, ILogger
{
    public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = new();

    public ILogger CreateLogger(string categoryName) => this;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var msg = formatter(state, exception);
        lock (Entries)
        {
            Entries.Add((logLevel, msg, exception));
        }
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public void Dispose() { }
}

public class CatalogMessagingPipelineIntegrationTests
{
    [Fact]
    public async Task CatalogPipeline_WhenCommandSucceeds_ShouldEmitInformationLogViaLoggingMiddleware()
    {
        var logSink = new TestLogSink();

        using var host = await Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddProvider(logSink);
                    logging.SetMinimumLevel(LogLevel.Debug);
                });
            })
            .UseWolverine(opts =>
            {
                opts.AddCatalogApplicationMessaging();
                opts.Discovery.DisableConventionalDiscovery();
                opts.Discovery.IncludeType<TestCatalogPipelineHandler>();
            })
            .StartAsync();

        var bus = host.Services.GetRequiredService<IMessageBus>();

        var result = await bus.InvokeAsync<string>(new PingCatalogCommand("Matrix"));

        result.Should().Be("Pong: Matrix");

        lock (logSink.Entries)
        {
            logSink.Entries.Should().Contain(e =>
                e.Level == LogLevel.Information &&
                e.Message.Contains("Handled request PingCatalogCommand") &&
                e.Message.Contains("Matrix"));

            // Ensure no false failure logs were emitted for successful command
            logSink.Entries.Should().NotContain(e =>
                e.Level == LogLevel.Warning &&
                e.Message.Contains("Request PingCatalogCommand failed with error"));
        }
    }

    [Fact]
    public async Task CatalogPipeline_WhenCommandFails_ShouldEmitWarningLogViaLoggingMiddleware()
    {
        var logSink = new TestLogSink();

        using var host = await Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddProvider(logSink);
                    logging.SetMinimumLevel(LogLevel.Debug);
                });
            })
            .UseWolverine(opts =>
            {
                opts.AddCatalogApplicationMessaging();
                opts.Discovery.DisableConventionalDiscovery();
                opts.Discovery.IncludeType<TestCatalogPipelineHandler>();
            })
            .StartAsync();

        var bus = host.Services.GetRequiredService<IMessageBus>();

        var act = async () => await bus.InvokeAsync(new FailingCatalogCommand());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Simulated Catalog Failure");

        lock (logSink.Entries)
        {
            logSink.Entries.Should().Contain(e =>
                e.Level == LogLevel.Warning &&
                e.Message.Contains("Request FailingCatalogCommand failed with error: Simulated Catalog Failure"));
        }
    }

    [Fact]
    public async Task CatalogPipeline_WhenCommandExceedsThreshold_ShouldEmitWarningLogViaPerformanceMiddleware()
    {
        var logSink = new TestLogSink();

        using var host = await Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddProvider(logSink);
                    logging.SetMinimumLevel(LogLevel.Debug);
                });
            })
            .UseWolverine(opts =>
            {
                opts.AddCatalogApplicationMessaging();
                opts.Discovery.DisableConventionalDiscovery();
                opts.Discovery.IncludeType<TestCatalogPipelineHandler>();
            })
            .StartAsync();

        var bus = host.Services.GetRequiredService<IMessageBus>();

        var result = await bus.InvokeAsync<string>(new SlowCatalogCommand(550));

        result.Should().Be("Done");

        lock (logSink.Entries)
        {
            logSink.Entries.Should().Contain(e =>
                e.Level == LogLevel.Warning &&
                e.Message.Contains("Long Running Request Detected: SlowCatalogCommand") &&
                e.Message.Contains("Threshold: 500 ms"));
        }
    }

    [Fact]
    public async Task CatalogPipeline_ShouldDiscoverRealApplicationHandlers()
    {
        using var host = await Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(Moq.Mock.Of<ShuffleSeries.Catalog.Domain.Repositories.IMoodRepository>());
                services.AddSingleton(Moq.Mock.Of<ShuffleSeries.Catalog.Application.Common.Interfaces.ICatalogCacheService>());
                services.AddSingleton(Moq.Mock.Of<ShuffleSeries.Shared.Core.Application.Common.Interfaces.ILanguageContext>());
            })
            .UseWolverine(opts =>
            {
                opts.AddCatalogApplicationMessaging();
            })
            .StartAsync();

        var bus = host.Services.GetRequiredService<IMessageBus>();
        var act = async () => await bus.InvokeAsync<IReadOnlyList<ShuffleSeries.Catalog.Application.Features.Moods.Responses.MoodResponse>>(
            new ShuffleSeries.Catalog.Application.Features.Moods.Queries.GetActiveMoods.GetActiveMoodsQuery());
        await act.Should().NotThrowAsync<Wolverine.Runtime.Routing.IndeterminateRoutesException>();
    }
}
