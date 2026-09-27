namespace ShuffleSeries.Shared.Core.Web.Observability;

/// <summary>
/// Configuration options for OpenTelemetry distributed tracing and metrics.
/// </summary>
public sealed class OpenTelemetryOptions
{
    public const string SectionName = "OpenTelemetry";

    /// <summary>
    /// Whether OpenTelemetry instrumentation is enabled. Default true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The OTLP endpoint (e.g. http://localhost:4317 or http://jaeger:4317).
    /// If null or empty, OTEL_EXPORTER_OTLP_ENDPOINT environment variable is checked.
    /// </summary>
    public string? OtlpEndpoint { get; set; }
}
