namespace Ordering.Application.Orders.Place;

public sealed record PlaceOrderLine(Guid ProductId, int Quantity);

public sealed record PlaceOrderCommand(
    string CustomerName,
    string CustomerEmail,
    IReadOnlyList<PlaceOrderLine> Items,
    bool SimulatePaymentFailure);
