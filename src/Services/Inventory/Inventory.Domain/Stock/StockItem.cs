using Store.SharedKernel;

namespace Inventory.Domain.Stock;

/// <summary>
/// Stock of one product. Invariant: 0 &lt;= QuantityReserved &lt;= QuantityOnHand, so Available is never negative.
/// Its identity is the Catalog product id (a reference, not a shared model).
/// </summary>
public sealed class StockItem : AggregateRoot<Guid>
{
    private StockItem()
    {
    }

    public string Sku { get; private set; } = string.Empty;

    public string ProductName { get; private set; } = string.Empty;

    public int QuantityOnHand { get; private set; }

    public int QuantityReserved { get; private set; }

    public int Available => QuantityOnHand - QuantityReserved;

    public static Result<StockItem> Register(Guid productId, string sku, string productName, int initialQuantity)
    {
        if (initialQuantity < 0)
        {
            return StockErrors.InvalidInitialQuantity;
        }

        return new StockItem
        {
            Id = productId,
            Sku = sku,
            ProductName = productName,
            QuantityOnHand = initialQuantity,
        };
    }

    public void Rename(string productName) => ProductName = productName;

    public Result Restock(int quantity)
    {
        if (quantity <= 0)
        {
            return StockErrors.InvalidQuantity;
        }

        QuantityOnHand += quantity;
        return Result.Success();
    }

    public Result Reserve(int quantity)
    {
        if (quantity <= 0)
        {
            return StockErrors.InvalidQuantity;
        }

        if (quantity > Available)
        {
            return StockErrors.Insufficient(Sku, quantity, Available);
        }

        QuantityReserved += quantity;
        return Result.Success();
    }

    /// <summary>Payment approved: reserved units leave the warehouse.</summary>
    public void CommitReservation(int quantity)
    {
        EnsureReserved(quantity);
        QuantityReserved -= quantity;
        QuantityOnHand -= quantity;
    }

    /// <summary>Compensation: reserved units become available again.</summary>
    public void ReleaseReservation(int quantity)
    {
        EnsureReserved(quantity);
        QuantityReserved -= quantity;
    }

    private void EnsureReserved(int quantity)
    {
        if (quantity <= 0 || quantity > QuantityReserved)
        {
            throw new DomainException(
                $"Cannot settle {quantity} unit(s) of {Sku}: only {QuantityReserved} reserved.");
        }
    }
}
