using System.Reflection;
using Microsoft.Extensions.Logging;
using Quartz;
using ShuffleSeries.Catalog.Domain.Events;
using ShuffleSeries.Catalog.Infrastructure.Persistence;
using ShuffleSeries.Shared.Core.Domain.Events;
using ShuffleSeries.Shared.Core.Domain.Outbox;
using ShuffleSeries.Shared.Core.Domain.Primitives;
using ShuffleSeries.Shared.Core.Infrastructure.Outbox;
using Wolverine;

namespace ShuffleSeries.Catalog.Infrastructure.BackgroundJobs;

[DisallowConcurrentExecution]
public sealed class ProcessOutboxMessagesJob : IJob
{
    private static readonly Assembly _catalogDomainAssembly = typeof(MediaCreatedDomainEvent).Assembly;

    private readonly CatalogDbContext _dbContext;
    private readonly IMessageBus _messageBus;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ProcessOutboxMessagesJob> _logger;

    public const int MaxRetries = OutboxBatchProcessor.DefaultMaxRetries;

    public ProcessOutboxMessagesJob(
        CatalogDbContext dbContext,
        IMessageBus messageBus,
        TimeProvider timeProvider,
        ILogger<ProcessOutboxMessagesJob> logger)
    {
        _dbContext = dbContext;
        _messageBus = messageBus;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context?.CancellationToken ?? CancellationToken.None;

        await OutboxBatchProcessor.ProcessBatchAsync(
            _dbContext,
            _catalogDomainAssembly,
            _timeProvider,
            _logger,
            PublishIntegrationEventAsync,
            batchSize: OutboxBatchProcessor.DefaultBatchSize,
            maxRetries: MaxRetries,
            retentionDays: OutboxBatchProcessor.DefaultRetentionDays,
            cancellationToken: cancellationToken);
    }

    private async Task<string> PublishIntegrationEventAsync(
        IDomainEvent domainEvent,
        OutboxMessage outboxMessage,
        CancellationToken cancellationToken)
    {
        Task PublishWithTimeoutAsync<T>(T message) =>
            _messageBus.PublishAsync(message).AsTask().WaitAsync(cancellationToken);

        switch (domainEvent)
        {
            case MediaCreatedDomainEvent e:
                var mediaCreated = new MediaCreatedEvent(
                    outboxMessage.Id,
                    e.MediaId,
                    e.MediaType.ToString(),
                    outboxMessage.OccurredOnUtc);
                await PublishWithTimeoutAsync(mediaCreated);
                return nameof(MediaCreatedEvent);

            case MediaUpdatedDomainEvent e:
                var mediaUpdated = new MediaUpdatedEvent(
                    outboxMessage.Id,
                    e.MediaId,
                    e.MediaType.ToString(),
                    outboxMessage.OccurredOnUtc);
                await PublishWithTimeoutAsync(mediaUpdated);
                return nameof(MediaUpdatedEvent);

            case MediaDeletedDomainEvent e:
                var mediaDeleted = new MediaDeletedEvent(
                    outboxMessage.Id,
                    e.MediaId,
                    e.MediaType.ToString(),
                    outboxMessage.OccurredOnUtc);
                await PublishWithTimeoutAsync(mediaDeleted);
                return nameof(MediaDeletedEvent);

            case EpisodeCreatedDomainEvent e:
                var epCreated = new EpisodeCreatedEvent(
                    outboxMessage.Id,
                    e.SeriesId,
                    e.SeasonId,
                    e.EpisodeId,
                    e.EpisodeNumber,
                    e.IsStandalone,
                    outboxMessage.OccurredOnUtc);
                await PublishWithTimeoutAsync(epCreated);
                return nameof(EpisodeCreatedEvent);

            case EpisodeUpdatedDomainEvent e:
                var epUpdated = new EpisodeUpdatedEvent(
                    outboxMessage.Id,
                    e.EpisodeId,
                    e.EpisodeNumber,
                    e.IsStandalone,
                    e.RuntimeMinutes,
                    e.ImdbRating,
                    outboxMessage.OccurredOnUtc);
                await PublishWithTimeoutAsync(epUpdated);
                return nameof(EpisodeUpdatedEvent);

            case EpisodeDeletedDomainEvent e:
                var epDeleted = new EpisodeDeletedEvent(
                    outboxMessage.Id,
                    e.EpisodeId,
                    outboxMessage.OccurredOnUtc);
                await PublishWithTimeoutAsync(epDeleted);
                return nameof(EpisodeDeletedEvent);

            case PlatformCreatedDomainEvent:
            case PlatformUpdatedDomainEvent:
            case PlatformDeletedDomainEvent:
            case MoodCreatedDomainEvent:
            case MoodUpdatedDomainEvent:
            case MoodDeletedDomainEvent:
            case GenreCreatedDomainEvent:
            case GenreUpdatedDomainEvent:
            case GenreDeletedDomainEvent:
                var (taxType, entityId, action) = domainEvent switch
                {
                    PlatformCreatedDomainEvent p => ("Platform", p.PlatformId, "Created"),
                    PlatformUpdatedDomainEvent p => ("Platform", p.PlatformId, "Updated"),
                    PlatformDeletedDomainEvent p => ("Platform", p.PlatformId, "Deleted"),
                    MoodCreatedDomainEvent m => ("Mood", m.MoodId, "Created"),
                    MoodUpdatedDomainEvent m => ("Mood", m.MoodId, "Updated"),
                    MoodDeletedDomainEvent m => ("Mood", m.MoodId, "Deleted"),
                    GenreCreatedDomainEvent g => ("Genre", g.GenreId, "Created"),
                    GenreUpdatedDomainEvent g => ("Genre", g.GenreId, "Updated"),
                    _ => ("Genre", ((GenreDeletedDomainEvent)domainEvent).GenreId, "Deleted")
                };
                var taxEvent = new CatalogTaxonomyChangedEvent(outboxMessage.Id, taxType, entityId, action, outboxMessage.OccurredOnUtc);
                await PublishWithTimeoutAsync(taxEvent);
                return nameof(CatalogTaxonomyChangedEvent);

            default:
                throw new InvalidOperationException(
                    $"Domain event '{domainEvent.GetType().Name}' has no mapped integration event contract. Direct publishing of internal domain events across service boundaries is strictly prohibited.");
        }
    }
}
