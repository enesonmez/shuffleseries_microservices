namespace ShuffleSeries.Shared.Core.Web.Correlation;

/// <summary>
/// Scoped thread-safe implementation of ICorrelationIdContext.
/// </summary>
public sealed class CorrelationIdContext : ICorrelationIdContext
{
    private string _correlationId = Guid.NewGuid().ToString("N");

    public string CorrelationId => _correlationId;

    public void SetCorrelationId(string correlationId)
    {
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            _correlationId = correlationId;
        }
    }
}
