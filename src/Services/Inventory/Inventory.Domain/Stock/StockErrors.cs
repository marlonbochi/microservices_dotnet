using Store.SharedKernel;

namespace Inventory.Domain.Stock;

public static class StockErrors
{
    public static readonly Error InvalidQuantity = Error.Validation(
        "Stock.InvalidQuantity", "Quantity must be greater than zero.");

    public static readonly Error InvalidInitialQuantity = Error.Validation(
        "Stock.InvalidInitialQuantity", "Initial quantity cannot be negative.");

    public static Error NotFound(Guid productId) => Error.NotFound(
        "Stock.NotFound", $"No stock record for product '{productId}'.");

    public static Error Insufficient(string sku, int requested, int available) => Error.Conflict(
        "Stock.Insufficient", $"Insufficient stock for {sku}: requested {requested}, available {available}.");

    public static Error UnknownProduct(Guid productId) => Error.Conflict(
        "Stock.UnknownProduct", $"Insufficient stock: product '{productId}' has no stock record.");
}
