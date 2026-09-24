using Microsoft.Extensions.DependencyInjection;
using Store.SharedKernel;

namespace Ordering.Infrastructure.DomainEvents;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken);
}

/// <summary>Resolves every <see cref="IDomainEventHandler{TEvent}"/> registered for the runtime event type.</summary>
internal sealed class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    private const string HandleMethodName = nameof(IDomainEventHandler<IDomainEvent>.HandleAsync);

    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in domainEvents)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var handleMethod = handlerType.GetMethod(HandleMethodName)!;
            foreach (var handler in serviceProvider.GetServices(handlerType))
            {
                await (Task)handleMethod.Invoke(handler, [domainEvent, cancellationToken])!;
            }
        }
    }
}
