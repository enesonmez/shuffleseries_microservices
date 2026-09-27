using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace ShuffleSeries.Shared.Core.Web.Logging;

/// <summary>
/// Extension methods for configuring Serilog with structured JSON logging, standard enrichers,
/// and extensible sink configurators (Console, File, PostgreSQL, and custom sinks).
/// </summary>
public static class SerilogExtensions
{
    /// <summary>
    /// Configures Serilog on the host with standard application metadata, LogContext enrichment,
    /// and built-in plus custom sink configurators.
    /// </summary>
    /// <param name="hostBuilder">The host builder to configure.</param>
    /// <param name="applicationName">The logical service name (e.g. Catalog.Api, Identity.Api).</param>
    /// <param name="configureBuilder">Optional builder action for registering custom sinks or rules.</param>
    /// <returns>The configured host builder.</returns>
    public static IHostBuilder UseSharedSerilog(
        this IHostBuilder hostBuilder,
        string applicationName,
        Action<SerilogLoggingBuilder>? configureBuilder = null)
    {
        var loggingBuilder = new SerilogLoggingBuilder();

        // Built-in sinks: Console, File, PostgreSQL
        loggingBuilder.AddSink<ConsoleSinkConfigurator>();
        loggingBuilder.AddSink<FileSinkConfigurator>();
        loggingBuilder.AddSink<PostgreSqlSinkConfigurator>();

        configureBuilder?.Invoke(loggingBuilder);

        return hostBuilder.UseSerilog((context, services, loggerConfiguration) =>
        {
            loggerConfiguration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .MinimumLevel.Override("System.Net.Http", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", applicationName)
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
                .Enrich.WithProperty("MachineName", Environment.MachineName)
                .Enrich.WithProperty("ProcessId", Environment.ProcessId)
                .Destructure.With<SensitiveDataDestructuringPolicy>()
                .ReadFrom.Configuration(context.Configuration);

            foreach (var configurator in loggingBuilder.SinkConfigurators)
            {
                configurator.Configure(loggerConfiguration, context.Configuration, context.HostingEnvironment);
            }
        });
    }
}
