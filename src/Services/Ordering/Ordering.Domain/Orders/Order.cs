using Store.SharedKernel;

namespace Ordering.Domain.Orders;

/// <summary>
/// The business record of a purchase. Its status is driven by the order saga, but the rules about
/// which transitions are legal live here, in the domain.
/// </summary>
public sealed class Order : AggregateRoot<Guid>
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
    {
        [OrderStatus.Submitted] = [OrderStatus.StockReserved, OrderStatus.Rejected],
        [OrderStatus.StockReserved] = [OrderStatus.PaymentApproved, OrderStatus.PaymentDeclined],
        [OrderStatus.PaymentApproved] = [OrderStatus.Confirmed],
        [OrderStatus.PaymentDeclined] = [OrderStatus.Cancelled],
        [OrderStatus.Confirmed] = [],
        [OrderStatus.Rejected] = [],
        [OrderStatus.Cancelled] = [],
    };

    private readonly List<OrderItem> _items = [];
    private readonly List<OrderStatusChange> _history = [];

    private Order()
    {
    }

    public string CustomerName { get; private set; } = string.Empty;

    public string CustomerEmail { get; private set; } = string.Empty;

    public OrderStatus Status { get; private set; }

    public string? FailureReason { get; private set; }

    public decimal Total { get; private set; }

    public bool SimulatePaymentFailure { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    public IReadOnlyList<OrderStatusChange> History => _history.AsReadOnly();

    public bool IsFinal => AllowedTransitions[Status].Length == 0;

    public static Result<Order> Place(
        string customerName,
        string customerEmail,
        IEnumerable<OrderItem> items,
        bool simulatePaymentFailure,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(customerName) || string.IsNullOrWhiteSpace(customerEmail))
        {
            return OrderErrors.InvalidCustomer;
        }

        var merged = MergeSameProduct(items);
        if (merged.Count == 0)
        {
            return OrderErrors.NoItems;
        }

        if (merged.Any(item => item.Quantity < 1))
        {
            return OrderErrors.InvalidQuantity;
        }

        var order = new Order
        {
            Id = Guid.CreateVersion7(),
            CustomerName = customerName.Trim(),
            CustomerEmail = customerEmail.Trim().ToLowerInvariant(),
            SimulatePaymentFailure = simulatePaymentFailure,
            CreatedAt = now,
            Total = merged.Sum(item => item.LineTotal),
        };
        order._items.AddRange(merged);
        order.Record(OrderStatus.Submitted, null, now);
        return order;
    }

    public Result MarkStockReserved(DateTimeOffset now) => TransitionTo(OrderStatus.StockReserved, null, now);

    public Result Reject(string reason, DateTimeOffset now) => TransitionTo(OrderStatus.Rejected, reason, now);

    public Result MarkPaymentApproved(DateTimeOffset now) => TransitionTo(OrderStatus.PaymentApproved, null, now);

    public Result MarkPaymentDeclined(string reason, DateTimeOffset now) => TransitionTo(OrderStatus.PaymentDeclined, reason, now);

    public Result Confirm(DateTimeOffset now) => TransitionTo(OrderStatus.Confirmed, null, now);

    public Result Cancel(DateTimeOffset now) => TransitionTo(OrderStatus.Cancelled, FailureReason, now);

    private Result TransitionTo(OrderStatus target, string? reason, DateTimeOffset now)
    {
        if (Status == target)
        {
            return Result.Success(); // duplicate message: idempotent no-op
        }

        if (!AllowedTransitions[Status].Contains(target))
        {
            return OrderErrors.InvalidTransition(Status, target);
        }

        FailureReason = reason ?? FailureReason;
        Record(target, reason, now);
        return Result.Success();
    }

    private void Record(OrderStatus status, string? note, DateTimeOffset now)
    {
        Status = status;
        _history.Add(new OrderStatusChange(status, now, note));
        Raise(new OrderStatusChangedDomainEvent(Id, status, FailureReason, now));
    }

    private static List<OrderItem> MergeSameProduct(IEnumerable<OrderItem> items) =>
        [.. items
            .GroupBy(item => item.ProductId)
            .Select(group => group.First() with { Quantity = group.Sum(item => item.Quantity) })];
}
