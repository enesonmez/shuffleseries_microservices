using MediatR;
using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Shared.Core.Application.Events;

public sealed class DomainEventNotification<TDomainEvent> : INotification
    where TDomainEvent : IDomainEvent
{
    public TDomainEvent Event { get; }

    public DomainEventNotification(TDomainEvent domainEvent)
    {
        Event = domainEvent;
    }
}