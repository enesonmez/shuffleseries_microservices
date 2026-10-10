using System.Reflection;
using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
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
            await ProcessSingleMessageAsync(outboxMessage, context.CancellationToken);
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);
    }

    private async Task ProcessSingleMessageAsync(OutboxMessage outboxMessage, CancellationToken cancellationToken)
    {
        try
        {
            var eventType = DomainEventTypeCache.Resolve(_identityDomainAssembly, outboxMessage.Type);
            if (eventType is null)
            {
                RecordMissingTypeFailure(outboxMessage);
                return;
            }

            if (JsonSerializer.Deserialize(outboxMessage.Content, eventType) is not IDomainEvent domainEvent)
            {
                RecordDeserializationFailure(outboxMessage);
                return;
            }

            var publishTypeName = await PublishIntegrationEventAsync(domainEvent, outboxMessage, cancellationToken);

            outboxMessage.ProcessedOnUtc = _timeProvider.GetUtcNow().UtcDateTime;
            outboxMessage.Error = null;

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "Outbox message {MessageId} of type {MessageType} published successfully as {PublishType}.",
                    outboxMessage.Id,
                    outboxMessage.Type,
                    publishTypeName);
            }
        }
        catch (Exception ex)
        {
            RecordProcessingException(outboxMessage, ex);
        }
    }

    private void RecordMissingTypeFailure(OutboxMessage outboxMessage)
    {
        outboxMessage.Error = $"Not found event type: {outboxMessage.Type}";
        outboxMessage.RetryCount++;

        if (_logger.IsEnabled(LogLevel.Warning))
        {
            _logger.LogWarning(
                "Event type {MessageType} not found for outbox message {MessageId}. Retry: {RetryCount}",
                outboxMessage.Type,
                outboxMessage.Id,
                outboxMessage.RetryCount);
        }
    }

    private void RecordDeserializationFailure(OutboxMessage outboxMessage)
    {
        outboxMessage.Error = "The message content could not be deserialized.";
        outboxMessage.RetryCount++;

        if (_logger.IsEnabled(LogLevel.Warning))
        {
            _logger.LogWarning(
                "Message content could not be deserialized for outbox message {MessageId}. Retry: {RetryCount}",
                outboxMessage.Id,
                outboxMessage.RetryCount);
        }
    }

    private void RecordProcessingException(OutboxMessage outboxMessage, Exception ex)
    {
        outboxMessage.Error = ex.Message;
        outboxMessage.RetryCount++;

        if (_logger.IsEnabled(LogLevel.Error))
        {
            _logger.LogError(
                ex,
                "Failed to process outbox message {MessageId} of type {MessageType}. Retry: {RetryCount}",
                outboxMessage.Id,
                outboxMessage.Type,
                outboxMessage.RetryCount);
        }
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
