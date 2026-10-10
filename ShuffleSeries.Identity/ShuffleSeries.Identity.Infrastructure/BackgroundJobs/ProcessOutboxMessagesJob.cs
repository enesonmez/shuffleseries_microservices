using System.Reflection;
using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using ShuffleSeries.Identity.Domain.Events;
using ShuffleSeries.Identity.Infrastructure.Persistence;
using ShuffleSeries.Shared.Core.Domain.Events;
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

    public const int MaxRetries = 3;

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
        var messages = await _dbContext.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null && m.RetryCount < MaxRetries)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(20)
            .ToListAsync(context.CancellationToken);

        if (messages.Count == 0)
        {
            return;
        }

        foreach (var outboxMessage in messages)
        {
            try
            {
                var eventType = DomainEventTypeCache.Resolve(_identityDomainAssembly, outboxMessage.Type);

                if (eventType is null)
                {
                    outboxMessage.Error = $"Not found event type: {outboxMessage.Type}";
                    outboxMessage.RetryCount++;

                    _logger.LogWarning(
                        "Event type {MessageType} not found for outbox message {MessageId}. Retry: {RetryCount}",
                        outboxMessage.Type,
                        outboxMessage.Id,
                        outboxMessage.RetryCount);

                    continue;
                }

                var domainEvent = JsonSerializer.Deserialize(outboxMessage.Content, eventType) as IDomainEvent;

                if (domainEvent is null)
                {
                    outboxMessage.Error = "The message content could not be deserialized.";
                    outboxMessage.RetryCount++;

                    _logger.LogWarning(
                        "Message content could not be deserialized for outbox message {MessageId}. Retry: {RetryCount}",
                        outboxMessage.Id,
                        outboxMessage.RetryCount);

                    continue;
                }

                // Domain Event -> Public Integration Event Mapping
                object messageToPublish = domainEvent switch
                {
                    UserRegisteredDomainEvent e => new UserRegisteredEvent(
                        outboxMessage.Id,
                        e.UserId,
                        e.Email,
                        e.IsGuest,
                        outboxMessage.OccurredOnUtc),

                    UserMergedDomainEvent e => new UserMergedEvent(
                        outboxMessage.Id,
                        e.TargetUserId,
                        e.GuestUserId,
                        outboxMessage.OccurredOnUtc),

                    UserAccountDeletedDomainEvent e => new UserAccountDeletedEvent(
                        outboxMessage.Id,
                        e.UserId,
                        e.Email,
                        outboxMessage.OccurredOnUtc),

                    _ => domainEvent
                };

                var publishType = messageToPublish.GetType();
                await _publishEndpoint.Publish(messageToPublish, publishType, context.CancellationToken);

                outboxMessage.ProcessedOnUtc = _timeProvider.GetUtcNow().UtcDateTime;
                outboxMessage.Error = null;

                _logger.LogInformation(
                    "Outbox message {MessageId} of type {MessageType} published successfully as {PublishType}.",
                    outboxMessage.Id,
                    outboxMessage.Type,
                    publishType.Name);
            }
            catch (Exception ex)
            {
                outboxMessage.Error = ex.Message;
                outboxMessage.RetryCount++;

                _logger.LogError(
                    ex,
                    "Failed to process outbox message {MessageId} of type {MessageType}. Retry: {RetryCount}",
                    outboxMessage.Id,
                    outboxMessage.Type,
                    outboxMessage.RetryCount);
            }
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
