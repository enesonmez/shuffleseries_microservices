namespace ShuffleSeries.Shared.Core.Web.Correlation;

/// <summary>
/// Scoped ambient context holding the CorrelationId of the current execution flow.
/// </summary>
public interface ICorrelationIdContext
{
    /// <summary>
    /// Current correlation ID.
    /// </summary>
    string CorrelationId { get; }

    /// <summary>
    /// Sets or updates the correlation ID for the current execution context.
    /// </summary>
    void SetCorrelationId(string correlationId);
}
