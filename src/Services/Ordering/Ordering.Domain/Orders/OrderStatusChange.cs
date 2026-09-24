namespace Ordering.Domain.Orders;

/// <summary>An entry of the order timeline.</summary>
public sealed record OrderStatusChange(OrderStatus Status, DateTimeOffset OccurredAt, string? Note);
