namespace ShuffleSeries.Shared.Core.Web.Correlation;

/// <summary>
/// DelegatingHandler for HttpClient that automatically propagates the current CorrelationId
/// downstream to external APIs or other microservices via the X-Correlation-ID header.
/// </summary>
public sealed class CorrelationIdDelegatingHandler : DelegatingHandler
{
    private readonly ICorrelationIdContext _correlationIdContext;

    public CorrelationIdDelegatingHandler(ICorrelationIdContext correlationIdContext)
    {
        _correlationIdContext = correlationIdContext;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (!request.Headers.Contains(CorrelationIdConstants.HeaderName))
        {
            var correlationId = _correlationIdContext.CorrelationId;
            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                request.Headers.TryAddWithoutValidation(CorrelationIdConstants.HeaderName, correlationId);
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}
