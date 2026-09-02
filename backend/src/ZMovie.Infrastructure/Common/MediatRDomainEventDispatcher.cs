using MediatR;
using ZMovie.Application.Common;
using ZMovie.Domain.Common;

namespace ZMovie.Infrastructure.Common;

public sealed class MediatRDomainEventDispatcher(IPublisher publisher) : IDomainEventDispatcher
{
    public async Task DispatchEventsAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken ct = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
            var notification = Activator.CreateInstance(notificationType, domainEvent);

            if (notification is INotification mediatrNotification)
            {
                await publisher.Publish(mediatrNotification, ct);
            }
        }
    }
}
