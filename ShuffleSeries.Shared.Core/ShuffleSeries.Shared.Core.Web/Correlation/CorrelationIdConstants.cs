namespace ShuffleSeries.Shared.Core.Web.Correlation;

/// <summary>
/// Constants used across distributed systems for correlation tracking.
/// </summary>
public static class CorrelationIdConstants
{
    /// <summary>
    /// Standard HTTP header name for correlation ID propagation across services.
    /// </summary>
    public const string HeaderName = "X-Correlation-ID";

    /// <summary>
    /// Serilog LogContext property name for structured logs.
    /// </summary>
    public const string LogPropertyName = "CorrelationId";

    /// <summary>
    /// OpenTelemetry Activity tag name for W3C distributed tracing.
    /// </summary>
    public const string ActivityTagName = "correlation.id";
}
