using Inventory.Application.Reservations;
using Inventory.Domain.Reservations;
using Inventory.Domain.Stock;
using Inventory.UnitTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Store.Contracts.Inventory;
using Store.SharedKernel;

namespace Inventory.UnitTests.Application;

public sealed class SettleReservationHandlerTests
{
    private readonly InMemoryInventory _inventory = new();
    private readonly Guid _orderId = Guid.NewGuid();
    private readonly StockItem _item;

    public SettleReservationHandlerTests()
    {
        _item = _inventory.Seed(10);
        _item.Reserve(4);
        _inventory.Add(StockReservation.Create(_orderId, [new ReservationLine(_item.Id, 4)], DateTimeOffset.UnixEpoch));
    }

    private CommitStockHandler CommitHandler() => new(
        _inventory, _inventory, _inventory, _inventory, TimeProvider.System, NullLogger<CommitStockHandler>.Instance);

    private ReleaseStockHandler ReleaseHandler() => new(
        _inventory, _inventory, _inventory, _inventory, TimeProvider.System, NullLogger<ReleaseStockHandler>.Instance);

    [Fact]
    public async Task Commit_DeductsStockAndPublishesStockCommitted()
    {
        await CommitHandler().HandleAsync(new CommitStockCommand(_orderId), TestContext.Current.CancellationToken);

        _item.QuantityOnHand.ShouldBe(6);
        _item.QuantityReserved.ShouldBe(0);
        _inventory.Reservation(_orderId)!.Status.ShouldBe(ReservationStatus.Committed);
        _inventory.Published.ShouldHaveSingleItem().ShouldBe(new StockCommitted(_orderId));
    }

    [Fact]
    public async Task Release_RestoresAvailabilityAndPublishesStockReleased()
    {
        await ReleaseHandler().HandleAsync(new ReleaseStockCommand(_orderId), TestContext.Current.CancellationToken);

        _item.Available.ShouldBe(10);
        _inventory.Reservation(_orderId)!.Status.ShouldBe(ReservationStatus.Released);
        _inventory.Published.ShouldHaveSingleItem().ShouldBe(new StockReleased(_orderId));
    }

    [Fact]
    public async Task Release_Twice_RestoresStockOnlyOnce()
    {
        await ReleaseHandler().HandleAsync(new ReleaseStockCommand(_orderId), TestContext.Current.CancellationToken);
        await ReleaseHandler().HandleAsync(new ReleaseStockCommand(_orderId), TestContext.Current.CancellationToken);

        _item.Available.ShouldBe(10);
        _item.QuantityOnHand.ShouldBe(10);
        _inventory.Published.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Commit_UnknownOrder_ReturnsNotFoundAndPublishesNothing()
    {
        var result = await CommitHandler().HandleAsync(new CommitStockCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.Error!.Type.ShouldBe(ErrorType.NotFound);
        _inventory.Published.ShouldBeEmpty();
    }
}
