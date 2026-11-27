using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;

public interface IInboxIdempotencyService
{
    Task<bool> ExecuteIdempotentAsync<TEvent>(
        TEvent integrationEvent,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;
}
