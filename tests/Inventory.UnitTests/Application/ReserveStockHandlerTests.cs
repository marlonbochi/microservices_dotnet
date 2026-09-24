using Inventory.Application.Reservations;
using Inventory.Domain.Reservations;
using Inventory.UnitTests.Fakes;
using Store.Contracts.Inventory;

namespace Inventory.UnitTests.Application;

public sealed class ReserveStockHandlerTests
{
    private readonly InMemoryInventory _inventory = new();
    private readonly ReserveStockHandler _handler;

    public ReserveStockHandlerTests() =>
        _handler = new ReserveStockHandler(_inventory, _inventory, _inventory, _inventory, TimeProvider.System);

    private Task<Store.SharedKernel.Result> Reserve(Guid orderId, params ReservationLine[] lines) =>
        _handler.HandleAsync(new ReserveStockCommand(orderId, lines), TestContext.Current.CancellationToken);

    [Fact]
    public async Task HandleAsync_WithEnoughStock_ReservesAndPublishesStockReserved()
    {
        var item = _inventory.Seed(10);
        var orderId = Guid.NewGuid();

        var result = await Reserve(orderId, new ReservationLine(item.Id, 3));

        result.IsSuccess.ShouldBeTrue();
        item.Available.ShouldBe(7);
        _inventory.Reservation(orderId)!.Status.ShouldBe(ReservationStatus.Reserved);
        _inventory.Published.ShouldHaveSingleItem().ShouldBe(new StockReserved(orderId));
    }

    [Fact]
    public async Task HandleAsync_WithInsufficientStock_PublishesFailureAndReservesNothing()
    {
        var item = _inventory.Seed(10);
        var orderId = Guid.NewGuid();

        var result = await Reserve(orderId, new ReservationLine(item.Id, 50));

        result.IsFailure.ShouldBeTrue();
        item.Available.ShouldBe(10);
        _inventory.Reservation(orderId).ShouldBeNull();
        var failed = _inventory.Published.ShouldHaveSingleItem().ShouldBeOfType<StockReservationFailed>();
        failed.Reason.ShouldContain("Insufficient stock");
    }

    [Fact]
    public async Task HandleAsync_WhenOneOfManyLinesLacksStock_IsAllOrNothing()
    {
        var plenty = _inventory.Seed(10, "A");
        var scarce = _inventory.Seed(1, "B");

        await Reserve(Guid.NewGuid(), new ReservationLine(plenty.Id, 2), new ReservationLine(scarce.Id, 2));

        plenty.Available.ShouldBe(10);
        scarce.Available.ShouldBe(1);
        _inventory.Published.ShouldHaveSingleItem().ShouldBeOfType<StockReservationFailed>();
    }

    [Fact]
    public async Task HandleAsync_WithUnknownProduct_TreatsAsInsufficientStock()
    {
        await Reserve(Guid.NewGuid(), new ReservationLine(Guid.NewGuid(), 1));

        _inventory.Published.ShouldHaveSingleItem().ShouldBeOfType<StockReservationFailed>();
    }

    [Fact]
    public async Task HandleAsync_WithDuplicateProductLines_MergesQuantities()
    {
        var item = _inventory.Seed(5);

        var result = await Reserve(Guid.NewGuid(), new ReservationLine(item.Id, 3), new ReservationLine(item.Id, 3));

        result.IsFailure.ShouldBeTrue();
        item.Available.ShouldBe(5);
    }

    [Fact]
    public async Task HandleAsync_SameOrderTwice_ReservesOnlyOnceAndAcknowledgesBoth()
    {
        var item = _inventory.Seed(10);
        var orderId = Guid.NewGuid();

        await Reserve(orderId, new ReservationLine(item.Id, 3));
        await Reserve(orderId, new ReservationLine(item.Id, 3));

        item.Available.ShouldBe(7);
        _inventory.Published.ShouldAllBe(message => message is StockReserved);
        _inventory.Published.Count.ShouldBe(2);
    }
}
