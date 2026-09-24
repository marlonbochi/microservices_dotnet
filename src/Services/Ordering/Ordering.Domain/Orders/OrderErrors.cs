using Store.SharedKernel;

namespace Ordering.Domain.Orders;

public static class OrderErrors
{
    public static readonly Error NoItems = Error.Validation("Order.NoItems", "An order needs at least one item.");

    public static readonly Error InvalidQuantity = Error.Validation("Order.InvalidQuantity", "Quantities must be at least 1.");

    public static readonly Error InvalidCustomer = Error.Validation("Order.InvalidCustomer", "Customer name and e-mail are required.");

    public static Error NotFound(Guid id) => Error.NotFound("Order.NotFound", $"Order '{id}' was not found.");

    public static Error InvalidTransition(OrderStatus from, OrderStatus to) =>
        Error.Conflict("Order.InvalidTransition", $"Cannot move an order from {from} to {to}.");
}
