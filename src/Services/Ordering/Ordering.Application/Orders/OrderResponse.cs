using Ordering.Domain.Orders;

namespace Ordering.Application.Orders;

public sealed record OrderItemResponse(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal LineTotal);

public sealed record OrderStatusChangeResponse(OrderStatus Status, DateTimeOffset OccurredAt, string? Note);

public sealed record OrderResponse(
    Guid Id,
    string CustomerName,
    string CustomerEmail,
    OrderStatus Status,
    string? FailureReason,
    decimal Total,
    bool SimulatePaymentFailure,
    DateTimeOffset CreatedAt,
    IReadOnlyList<OrderItemResponse> Items,
    IReadOnlyList<OrderStatusChangeResponse> History);

internal static class OrderMappings
{
    public static OrderResponse ToResponse(this Order order) => new(
        order.Id,
        order.CustomerName,
        order.CustomerEmail,
        order.Status,
        order.FailureReason,
        order.Total,
        order.SimulatePaymentFailure,
        order.CreatedAt,
        [.. order.Items.Select(item => new OrderItemResponse(item.ProductId, item.ProductName, item.UnitPrice, item.Quantity, item.LineTotal))],
        [.. order.History.Select(change => new OrderStatusChangeResponse(change.Status, change.OccurredAt, change.Note))]);
}
