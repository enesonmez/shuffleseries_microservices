using System.Reflection;
using MassTransit;
using Microsoft.Extensions.Logging;
using Quartz;
using ShuffleSeries.Catalog.Domain.Events;
using ShuffleSeries.Catalog.Infrastructure.Persistence;
using ShuffleSeries.Shared.Core.Domain.Events;
using ShuffleSeries.Shared.Core.Domain.Outbox;
using ShuffleSeries.Shared.Core.Domain.Primitives;
using ShuffleSeries.Shared.Core.Infrastructure.Outbox;

namespace ShuffleSeries.Catalog.Infrastructure.BackgroundJobs;

[DisallowConcurrentExecution]
public sealed class ProcessOutboxMessagesJob : IJob
{
    private static readonly Assembly _catalogDomainAssembly = typeof(MediaCreatedDomainEvent).Assembly;

    private readonly CatalogDbContext _dbContext;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ProcessOutboxMessagesJob> _logger;

    public const int MaxRetries = OutboxBatchProcessor.DefaultMaxRetries;

    public ProcessOutboxMessagesJob(
        CatalogDbContext dbContext,
        IPublishEndpoint publishEndpoint,
        TimeProvider timeProvider,
        ILogger<ProcessOutboxMessagesJob> logger)
    {
        _dbContext = dbContext;
        _publishEndpoint = publishEndpoint;
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
        switch (domainEvent)
        {
            case MediaCreatedDomainEvent e:
                var mediaCreated = new MediaCreatedEvent(
                    outboxMessage.Id,
                    e.MediaId,
                    e.MediaType.ToString(),
                    outboxMessage.OccurredOnUtc);
                await _publishEndpoint.Publish(mediaCreated, cancellationToken);
                return nameof(MediaCreatedEvent);

            case MediaUpdatedDomainEvent e:
                var mediaUpdated = new MediaUpdatedEvent(
                    outboxMessage.Id,
                    e.MediaId,
                    e.MediaType.ToString(),
                    outboxMessage.OccurredOnUtc);
                await _publishEndpoint.Publish(mediaUpdated, cancellationToken);
                return nameof(MediaUpdatedEvent);

            case MediaDeletedDomainEvent e:
                var mediaDeleted = new MediaDeletedEvent(
                    outboxMessage.Id,
                    e.MediaId,
                    e.MediaType.ToString(),
                    outboxMessage.OccurredOnUtc);
                await _publishEndpoint.Publish(mediaDeleted, cancellationToken);
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
                await _publishEndpoint.Publish(epCreated, cancellationToken);
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
                await _publishEndpoint.Publish(epUpdated, cancellationToken);
                return nameof(EpisodeUpdatedEvent);

            case EpisodeDeletedDomainEvent e:
                var epDeleted = new EpisodeDeletedEvent(
                    outboxMessage.Id,
                    e.EpisodeId,
                    outboxMessage.OccurredOnUtc);
                await _publishEndpoint.Publish(epDeleted, cancellationToken);
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
                await _publishEndpoint.Publish(taxEvent, cancellationToken);
                return nameof(CatalogTaxonomyChangedEvent);

            default:
                await _publishEndpoint.Publish(domainEvent, domainEvent.GetType(), cancellationToken);
                return domainEvent.GetType().Name;
        }
    }
}
