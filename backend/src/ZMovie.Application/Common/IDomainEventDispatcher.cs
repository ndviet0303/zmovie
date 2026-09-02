using ZMovie.Domain.Common;

namespace ZMovie.Application.Common;

public interface IDomainEventDispatcher
{
    Task DispatchEventsAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken ct = default);
}
