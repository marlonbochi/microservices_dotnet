namespace Store.Contracts.Ordering;

public sealed record OrderLine(Guid ProductId, int Quantity);

/// <summary>Published by Ordering when a new order is accepted. Starts the order saga.</summary>
public sealed record OrderSubmitted(
    Guid OrderId,
    string CustomerEmail,
    decimal Total,
    bool SimulatePaymentFailure,
    IReadOnlyList<OrderLine> Items,
    DateTimeOffset OccurredAt);
