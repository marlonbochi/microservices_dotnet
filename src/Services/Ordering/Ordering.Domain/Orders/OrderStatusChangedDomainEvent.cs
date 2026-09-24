using Store.SharedKernel;

namespace Ordering.Domain.Orders;

public sealed record OrderStatusChangedDomainEvent(
    Guid OrderId,
    OrderStatus Status,
    string? FailureReason,
    DateTimeOffset OccurredAt) : IDomainEvent;
