using MediatR;
using ShuffleSeries.Shared.Core.Application.Events;
using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Shared.Core.Application.Extensions;

public static class MediatorExtensions
{
    public static async Task PublishDomainEventAsync(
        this IPublisher mediator, 
        IDomainEvent domainEvent, 
        CancellationToken cancellationToken = default)
    {
        var wrapperType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
        
        var notification = Activator.CreateInstance(wrapperType, domainEvent);

        if (notification is INotification mediatrNotification)
        {
            await mediator.Publish(mediatrNotification, cancellationToken);
        }
    }
}