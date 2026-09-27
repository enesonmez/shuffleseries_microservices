using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace ShuffleSeries.Shared.Core.Web.Logging;

/// <summary>
/// Builder for registering extensible Serilog sinks and customizations.
/// Allows adding custom sinks (e.g. Seq, Loki, Elasticsearch, Azure Monitor) seamlessly.
/// </summary>
public sealed class SerilogLoggingBuilder
{
    private readonly List<ILogSinkConfigurator> _sinkConfigurators = [];

    public IReadOnlyList<ILogSinkConfigurator> SinkConfigurators => _sinkConfigurators.AsReadOnly();

    /// <summary>
    /// Adds an instance of ILogSinkConfigurator.
    /// </summary>
    public SerilogLoggingBuilder AddSink(ILogSinkConfigurator configurator)
    {
        _sinkConfigurators.Add(configurator);
        return this;
    }

    /// <summary>
    /// Adds a typed ILogSinkConfigurator.
    /// </summary>
    public SerilogLoggingBuilder AddSink<T>() where T : ILogSinkConfigurator, new()
    {
        _sinkConfigurators.Add(new T());
        return this;
    }

    /// <summary>
    /// Adds an inline action to configure Serilog with a custom sink or rules.
    /// </summary>
    public SerilogLoggingBuilder AddCustomConfiguration(Action<LoggerConfiguration, IConfiguration, IHostEnvironment> action)
    {
        _sinkConfigurators.Add(new ActionSinkConfigurator(action));
        return this;
    }

    private sealed class ActionSinkConfigurator : ILogSinkConfigurator
    {
        private readonly Action<LoggerConfiguration, IConfiguration, IHostEnvironment> _action;

        public ActionSinkConfigurator(Action<LoggerConfiguration, IConfiguration, IHostEnvironment> action)
        {
            _action = action;
        }

        public void Configure(LoggerConfiguration loggerConfiguration, IConfiguration configuration, IHostEnvironment environment) =>
            _action(loggerConfiguration, configuration, environment);
    }
}
