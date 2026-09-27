using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Compact;

namespace ShuffleSeries.Shared.Core.Web.Logging;

/// <summary>
/// Configures Serilog Console sink with structured JSON output or human-readable format.
/// </summary>
public sealed class ConsoleSinkConfigurator : ILogSinkConfigurator
{
    public void Configure(
        LoggerConfiguration loggerConfiguration,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var sinksOptions = configuration.GetSection(LoggingSinksOptions.SectionName).Get<LoggingSinksOptions>()
                           ?? new LoggingSinksOptions();

        if (!sinksOptions.Console.Enabled)
        {
            return;
        }

        if (sinksOptions.Console.UseJsonFormat)
        {
            loggerConfiguration.WriteTo.Async(a => a.Console(new CompactJsonFormatter()));
        }
        else
        {
            loggerConfiguration.WriteTo.Async(a => a.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}"));
        }
    }
}
