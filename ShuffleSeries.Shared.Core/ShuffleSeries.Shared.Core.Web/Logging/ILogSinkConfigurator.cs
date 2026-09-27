using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace ShuffleSeries.Shared.Core.Web.Logging;

/// <summary>
/// Strategy contract for configuring an extensible Serilog logging sink (e.g. Console, File, PostgreSQL, Seq, Loki).
/// Allows easily plugging in new sink platforms by implementing this interface without altering the core logging setup.
/// </summary>
public interface ILogSinkConfigurator
{
    /// <summary>
    /// Configures the Serilog logger configuration with the destination sink based on application settings and environment.
    /// </summary>
    /// <param name="loggerConfiguration">The Serilog configuration being constructed.</param>
    /// <param name="configuration">The application configuration (appsettings / Vault / Env vars).</param>
    /// <param name="environment">The host environment (Development / Production).</param>
    void Configure(
        LoggerConfiguration loggerConfiguration,
        IConfiguration configuration,
        IHostEnvironment environment);
}
