using Inventory.Domain.Stock;
using Store.SharedKernel;

namespace Inventory.UnitTests.Domain;

public sealed class StockItemTests
{
    private static StockItem NewItem(int onHand = 10) =>
        StockItem.Register(Guid.NewGuid(), "KB-001", "Teclado", onHand).Value;

    [Fact]
    public void Register_WithNegativeQuantity_ReturnsError()
    {
        StockItem.Register(Guid.NewGuid(), "KB-001", "Teclado", -1).Error.ShouldBe(StockErrors.InvalidInitialQuantity);
    }

    [Fact]
    public void Reserve_WithinAvailable_DecreasesAvailableButNotOnHand()
    {
        var item = NewItem();

        var result = item.Reserve(3);

        result.IsSuccess.ShouldBeTrue();
        item.QuantityOnHand.ShouldBe(10);
        item.QuantityReserved.ShouldBe(3);
        item.Available.ShouldBe(7);
    }

    [Fact]
    public void Reserve_MoreThanAvailable_FailsAndKeepsState()
    {
        var item = NewItem();
        item.Reserve(8);

        var result = item.Reserve(3);

        result.Error!.Code.ShouldBe("Stock.Insufficient");
        item.Available.ShouldBe(2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void Reserve_NonPositiveQuantity_Fails(int quantity)
    {
        NewItem().Reserve(quantity).Error.ShouldBe(StockErrors.InvalidQuantity);
    }

    [Fact]
    public void CommitReservation_DeductsOnHandAndReserved()
    {
        var item = NewItem();
        item.Reserve(4);

        item.CommitReservation(4);

        item.QuantityOnHand.ShouldBe(6);
        item.QuantityReserved.ShouldBe(0);
        item.Available.ShouldBe(6);
    }

    [Fact]
    public void ReleaseReservation_RestoresAvailability()
    {
        var item = NewItem();
        item.Reserve(4);

        item.ReleaseReservation(4);

        item.QuantityOnHand.ShouldBe(10);
        item.Available.ShouldBe(10);
    }

    [Fact]
    public void ReleaseReservation_MoreThanReserved_Throws()
    {
        var item = NewItem();
        item.Reserve(1);

        Should.Throw<DomainException>(() => item.ReleaseReservation(2));
    }

    [Fact]
    public void Restock_PositiveQuantity_IncreasesOnHand()
    {
        var item = NewItem();

        item.Restock(5).IsSuccess.ShouldBeTrue();

        item.QuantityOnHand.ShouldBe(15);
    }

    [Fact]
    public void Restock_ZeroQuantity_Fails()
    {
        NewItem().Restock(0).Error.ShouldBe(StockErrors.InvalidQuantity);
    }
}
