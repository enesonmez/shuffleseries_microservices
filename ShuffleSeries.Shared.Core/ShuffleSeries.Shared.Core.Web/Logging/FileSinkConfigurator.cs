using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Compact;

namespace ShuffleSeries.Shared.Core.Web.Logging;

/// <summary>
/// Configures Serilog File sink with rolling interval and structured JSON output.
/// </summary>
public sealed class FileSinkConfigurator : ILogSinkConfigurator
{
    public void Configure(
        LoggerConfiguration loggerConfiguration,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var sinksOptions = configuration.GetSection(LoggingSinksOptions.SectionName).Get<LoggingSinksOptions>()
                           ?? new LoggingSinksOptions();

        if (!sinksOptions.File.Enabled)
        {
            return;
        }

        var path = sinksOptions.File.Path;
        var rollingInterval = sinksOptions.File.RollingInterval;
        var retainedCount = sinksOptions.File.RetainedFileCountLimit;

        if (sinksOptions.File.UseJsonFormat)
        {
            loggerConfiguration.WriteTo.Async(a => a.File(
                new CompactJsonFormatter(),
                path,
                rollingInterval: rollingInterval,
                retainedFileCountLimit: retainedCount));
        }
        else
        {
            loggerConfiguration.WriteTo.Async(a => a.File(
                path,
                rollingInterval: rollingInterval,
                retainedFileCountLimit: retainedCount,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}"));
        }
    }
}
