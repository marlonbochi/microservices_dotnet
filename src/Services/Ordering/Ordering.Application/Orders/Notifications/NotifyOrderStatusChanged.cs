using Ordering.Application.Abstractions;
using Ordering.Domain.Orders;
using Store.SharedKernel;

namespace Ordering.Application.Orders.Notifications;

/// <summary>Domain event handler: every status change is pushed to the browser.</summary>
public sealed class NotifyOrderStatusChanged(IOrderNotifier notifier) : IDomainEventHandler<OrderStatusChangedDomainEvent>
{
    public Task HandleAsync(OrderStatusChangedDomainEvent domainEvent, CancellationToken cancellationToken) =>
        notifier.NotifyStatusChangedAsync(
            new OrderStatusNotification(domainEvent.OrderId, domainEvent.Status, domainEvent.FailureReason, domainEvent.OccurredAt),
            cancellationToken);
}
