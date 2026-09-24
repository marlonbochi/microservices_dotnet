using Inventory.Domain.Stock;

namespace Inventory.Application.Stock;

public sealed record StockItemResponse(
    Guid ProductId,
    string Sku,
    string ProductName,
    int QuantityOnHand,
    int QuantityReserved,
    int Available);

internal static class StockItemMappings
{
    public static StockItemResponse ToResponse(this StockItem item) =>
        new(item.Id, item.Sku, item.ProductName, item.QuantityOnHand, item.QuantityReserved, item.Available);
}
