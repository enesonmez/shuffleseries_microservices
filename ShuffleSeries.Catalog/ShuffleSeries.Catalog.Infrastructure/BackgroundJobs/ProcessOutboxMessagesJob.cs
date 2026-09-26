using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Quartz;
using ShuffleSeries.Catalog.Infrastructure.Persistence;
using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Catalog.Infrastructure.BackgroundJobs;

[DisallowConcurrentExecution]
public class ProcessOutboxMessagesJob : IJob
{
    private readonly CatalogDbContext _dbContext;
    private readonly IPublishEndpoint _publishEndpoint;

    public ProcessOutboxMessagesJob(CatalogDbContext dbContext, IPublishEndpoint publishEndpoint)
    {
        _dbContext = dbContext;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var messages = await _dbContext.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null)
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
                var eventType = Type.GetType($"ShuffleSeries.Catalog.Domain.Events.{outboxMessage.Type}, ShuffleSeries.Catalog.Domain");

                if (eventType is null)
                {
                    outboxMessage.Error = $"Not found event type: {outboxMessage.Type}";
                    continue;
                }

                var domainEvent = JsonSerializer.Deserialize(outboxMessage.Content, eventType) as IDomainEvent;

                if (domainEvent is null)
                {
                    outboxMessage.Error = "The message content could not be deserialized.";
                    continue;
                }

                await _publishEndpoint.Publish(domainEvent, eventType, context.CancellationToken);

                outboxMessage.ProcessedOnUtc = DateTime.UtcNow;
                outboxMessage.Error = null;
            }
            catch (Exception ex)
            {
                outboxMessage.Error = ex.Message;
            }
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);
    }
}