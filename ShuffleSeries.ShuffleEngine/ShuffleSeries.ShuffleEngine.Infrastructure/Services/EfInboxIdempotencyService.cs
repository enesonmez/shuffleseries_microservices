using Microsoft.Extensions.Logging;
using ShuffleSeries.Shared.Core.Domain.Primitives;
using ShuffleSeries.Shared.Core.Infrastructure.Inbox;
using ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;
using ShuffleSeries.ShuffleEngine.Infrastructure.Persistence;

namespace ShuffleSeries.ShuffleEngine.Infrastructure.Services;

internal sealed class EfInboxIdempotencyService(
    ShuffleEngineDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<EfInboxIdempotencyService> logger) : IInboxIdempotencyService
{
    public Task<bool> ExecuteIdempotentAsync<TEvent>(
        TEvent integrationEvent,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent =>
        InboxProcessor.ExecuteIdempotentAsync(
            dbContext,
            integrationEvent,
            timeProvider,
            logger,
            action,
            InboxOptions.Default,
            cancellationToken);
}
