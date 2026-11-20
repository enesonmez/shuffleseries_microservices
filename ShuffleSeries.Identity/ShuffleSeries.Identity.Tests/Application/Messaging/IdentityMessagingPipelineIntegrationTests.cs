using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShuffleSeries.Identity.Application;
using Wolverine;

namespace ShuffleSeries.Identity.Tests.Application.Messaging;

public class IdentityMessagingPipelineIntegrationTests
{
    public sealed record PingIdentityCommand(string Username);
    public sealed record SlowIdentityCommand(int DelayMs);
    public sealed record FailingIdentityCommand;

    public sealed class TestIdentityPipelineHandler
    {
        public static string Handle(PingIdentityCommand cmd) => $"Pong: {cmd.Username}";

        public static async Task<string> Handle(SlowIdentityCommand cmd)
        {
            await Task.Delay(cmd.DelayMs);
            return "Done";
        }

        public static void Handle(FailingIdentityCommand cmd) =>
            throw new InvalidOperationException("Simulated Identity Failure");
    }

    private sealed class TestLogSink : ILoggerProvider, ILogger
    {
        public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

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

    [Fact]
    public async Task IdentityPipeline_WhenCommandSucceeds_ShouldEmitInformationLogViaLoggingMiddleware()
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
                opts.AddIdentityApplicationMessaging();
                opts.Discovery.DisableConventionalDiscovery();
                opts.Discovery.IncludeType<TestIdentityPipelineHandler>();
            })
            .StartAsync();

        var bus = host.Services.GetRequiredService<IMessageBus>();

        var result = await bus.InvokeAsync<string>(new PingIdentityCommand("Neo"));

        result.Should().Be("Pong: Neo");

        lock (logSink.Entries)
        {
            logSink.Entries.Should().Contain(e =>
                e.Level == LogLevel.Information &&
                e.Message.Contains("Handled request PingIdentityCommand") &&
                e.Message.Contains("Neo"));

            logSink.Entries.Should().NotContain(e =>
                e.Level == LogLevel.Warning &&
                e.Message.Contains("Request PingIdentityCommand failed with error"));
        }
    }

    [Fact]
    public async Task IdentityPipeline_WhenCommandFails_ShouldEmitWarningLogViaLoggingMiddleware()
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
                opts.AddIdentityApplicationMessaging();
                opts.Discovery.DisableConventionalDiscovery();
                opts.Discovery.IncludeType<TestIdentityPipelineHandler>();
            })
            .StartAsync();

        var bus = host.Services.GetRequiredService<IMessageBus>();

        var act = async () => await bus.InvokeAsync(new FailingIdentityCommand());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Simulated Identity Failure");

        lock (logSink.Entries)
        {
            logSink.Entries.Should().Contain(e =>
                e.Level == LogLevel.Warning &&
                e.Message.Contains("Request FailingIdentityCommand failed with error: Simulated Identity Failure"));
        }
    }

    [Fact]
    public async Task IdentityPipeline_WhenCommandExceedsThreshold_ShouldEmitWarningLogViaPerformanceMiddleware()
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
                opts.AddIdentityApplicationMessaging();
                opts.Discovery.DisableConventionalDiscovery();
                opts.Discovery.IncludeType<TestIdentityPipelineHandler>();
            })
            .StartAsync();

        var bus = host.Services.GetRequiredService<IMessageBus>();

        var result = await bus.InvokeAsync<string>(new SlowIdentityCommand(550));

        result.Should().Be("Done");

        lock (logSink.Entries)
        {
            logSink.Entries.Should().Contain(e =>
                e.Level == LogLevel.Warning &&
                e.Message.Contains("Long Running Request Detected: SlowIdentityCommand") &&
                e.Message.Contains("Threshold: 500 ms"));
        }
    }

    [Fact]
    public async Task IdentityPipeline_ShouldDiscoverRealApplicationHandlers()
    {
        using var host = await Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(Moq.Mock.Of<ShuffleSeries.Identity.Domain.Repositories.IUserRepository>());
                services.AddSingleton(Moq.Mock.Of<ShuffleSeries.Identity.Domain.Repositories.IRoleRepository>());
                services.AddSingleton(Moq.Mock.Of<ShuffleSeries.Identity.Application.Common.Interfaces.IAuthResponseService>());
            })
            .UseWolverine(opts =>
            {
                opts.AddIdentityApplicationMessaging();
            })
            .StartAsync();

        var bus = host.Services.GetRequiredService<IMessageBus>();
        var act = async () => await bus.InvokeAsync<AuthResponse>(new ShuffleSeries.Identity.Application.Features.Auth.Commands.CreateGuestSession.CreateGuestSessionCommand("127.0.0.1"));
        await act.Should().NotThrowAsync<Wolverine.Runtime.Routing.IndeterminateRoutesException>();
    }
}
