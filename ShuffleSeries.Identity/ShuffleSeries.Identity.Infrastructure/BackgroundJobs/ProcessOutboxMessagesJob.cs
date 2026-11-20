using System.Reflection;
using Microsoft.Extensions.Logging;
using Quartz;
using ShuffleSeries.Identity.Domain.Events;
using ShuffleSeries.Identity.Infrastructure.Persistence;
using ShuffleSeries.Shared.Core.Domain.Events;
using ShuffleSeries.Shared.Core.Domain.Outbox;
using ShuffleSeries.Shared.Core.Domain.Primitives;
using ShuffleSeries.Shared.Core.Infrastructure.Outbox;
using Wolverine;

namespace ShuffleSeries.Identity.Infrastructure.BackgroundJobs;

[DisallowConcurrentExecution]
public sealed class ProcessOutboxMessagesJob : IJob
{
    private static readonly Assembly _identityDomainAssembly = typeof(UserRegisteredDomainEvent).Assembly;

    private readonly IdentityDbContext _dbContext;
    private readonly IMessageBus _messageBus;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ProcessOutboxMessagesJob> _logger;

    public const int MaxRetries = OutboxBatchProcessor.DefaultMaxRetries;

    public ProcessOutboxMessagesJob(
        IdentityDbContext dbContext,
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
            _identityDomainAssembly,
            _timeProvider,
            _logger,
            PublishIntegrationEventAsync,
            new OutboxBatchOptions(MaxRetries: MaxRetries),
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
            case UserRegisteredDomainEvent e:
                var regEvent = new UserRegisteredEvent(
                    outboxMessage.Id,
                    e.UserId,
                    e.Email,
                    e.IsGuest,
                    outboxMessage.OccurredOnUtc);
                await PublishWithTimeoutAsync(regEvent);
                return nameof(UserRegisteredEvent);

            case UserAccountDeletedDomainEvent e:
                var delEvent = new UserAccountDeletedEvent(
                    outboxMessage.Id,
                    e.UserId,
                    e.Email,
                    outboxMessage.OccurredOnUtc);
                await PublishWithTimeoutAsync(delEvent);
                return nameof(UserAccountDeletedEvent);

            default:
                throw new InvalidOperationException(
                    $"Domain event '{domainEvent.GetType().Name}' has no mapped integration event contract. Direct publishing of internal domain events across service boundaries is strictly prohibited.");
        }
    }
}
