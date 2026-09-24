namespace Ordering.Domain.Orders;

/// <summary>
/// A line of the order. Name and price are copied from the catalog at order time (snapshot), so later
/// catalog changes never alter an existing order.
/// </summary>
public sealed record OrderItem(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity)
{
    public decimal LineTotal => UnitPrice * Quantity;
}
