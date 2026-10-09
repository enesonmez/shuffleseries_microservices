using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Quartz;
using ShuffleSeries.Identity.Infrastructure.Persistence;
using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Identity.Infrastructure.BackgroundJobs;

[DisallowConcurrentExecution]
public sealed class ProcessOutboxMessagesJob : IJob
{
    private readonly IdentityDbContext _dbContext;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly TimeProvider _timeProvider;

    private const int MaxRetries = 3;

    public ProcessOutboxMessagesJob(
        IdentityDbContext dbContext,
        IPublishEndpoint publishEndpoint,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _publishEndpoint = publishEndpoint;
        _timeProvider = timeProvider;
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
                var eventType = Type.GetType($"ShuffleSeries.Identity.Domain.Events.{outboxMessage.Type}, ShuffleSeries.Identity.Domain");

                if (eventType is null)
                {
                    outboxMessage.Error = $"Not found event type: {outboxMessage.Type}";
                    outboxMessage.RetryCount++;
                    continue;
                }

                var domainEvent = JsonSerializer.Deserialize(outboxMessage.Content, eventType) as IDomainEvent;

                if (domainEvent is null)
                {
                    outboxMessage.Error = "The message content could not be deserialized.";
                    outboxMessage.RetryCount++;
                    continue;
                }

                await _publishEndpoint.Publish(domainEvent, eventType, context.CancellationToken);

                outboxMessage.ProcessedOnUtc = _timeProvider.GetUtcNow().UtcDateTime;
                outboxMessage.Error = null;
            }
            catch (Exception ex)
            {
                outboxMessage.Error = ex.Message;
                outboxMessage.RetryCount++;
            }
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
