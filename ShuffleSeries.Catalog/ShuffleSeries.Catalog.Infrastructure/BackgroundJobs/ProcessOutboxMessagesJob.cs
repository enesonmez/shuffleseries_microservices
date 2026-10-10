using System.Reflection;
using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Quartz;
using ShuffleSeries.Catalog.Domain.Events;
using ShuffleSeries.Catalog.Infrastructure.Persistence;
using ShuffleSeries.Shared.Core.Domain.Primitives;
using ShuffleSeries.Shared.Core.Infrastructure.Outbox;

namespace ShuffleSeries.Catalog.Infrastructure.BackgroundJobs;

[DisallowConcurrentExecution]
public sealed class ProcessOutboxMessagesJob : IJob
{
    private static readonly Assembly _catalogDomainAssembly = typeof(SeriesCreatedDomainEvent).Assembly;

    private readonly CatalogDbContext _dbContext;
    private readonly IPublishEndpoint _publishEndpoint;

    public const int MaxRetries = 3;

    public ProcessOutboxMessagesJob(CatalogDbContext dbContext, IPublishEndpoint publishEndpoint)
    {
        _dbContext = dbContext;
        _publishEndpoint = publishEndpoint;
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
                var eventType = DomainEventTypeCache.Resolve(_catalogDomainAssembly, outboxMessage.Type);

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

                outboxMessage.ProcessedOnUtc = DateTime.UtcNow;
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
