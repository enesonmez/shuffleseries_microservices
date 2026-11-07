using System.Reflection;
using MassTransit;
using Microsoft.Extensions.Logging;
using Quartz;
using ShuffleSeries.Identity.Domain.Events;
using ShuffleSeries.Identity.Infrastructure.Persistence;
using ShuffleSeries.Shared.Core.Domain.Events;
using ShuffleSeries.Shared.Core.Domain.Outbox;
using ShuffleSeries.Shared.Core.Domain.Primitives;
using ShuffleSeries.Shared.Core.Infrastructure.Outbox;

namespace ShuffleSeries.Identity.Infrastructure.BackgroundJobs;

[DisallowConcurrentExecution]
public sealed class ProcessOutboxMessagesJob : IJob
{
    private static readonly Assembly _identityDomainAssembly = typeof(UserRegisteredDomainEvent).Assembly;

    private readonly IdentityDbContext _dbContext;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ProcessOutboxMessagesJob> _logger;

    public const int MaxRetries = OutboxBatchProcessor.DefaultMaxRetries;

    public ProcessOutboxMessagesJob(
        IdentityDbContext dbContext,
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
            _identityDomainAssembly,
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
            case UserRegisteredDomainEvent e:
                var regEvent = new UserRegisteredEvent(
                    outboxMessage.Id,
                    e.UserId,
                    e.Email,
                    e.IsGuest,
                    outboxMessage.OccurredOnUtc);
                await _publishEndpoint.Publish(regEvent, cancellationToken);
                return nameof(UserRegisteredEvent);

            case UserAccountDeletedDomainEvent e:
                var delEvent = new UserAccountDeletedEvent(
                    outboxMessage.Id,
                    e.UserId,
                    e.Email,
                    outboxMessage.OccurredOnUtc);
                await _publishEndpoint.Publish(delEvent, cancellationToken);
                return nameof(UserAccountDeletedEvent);

            default:
                await _publishEndpoint.Publish(domainEvent, domainEvent.GetType(), cancellationToken);
                return domainEvent.GetType().Name;
        }
    }
}
