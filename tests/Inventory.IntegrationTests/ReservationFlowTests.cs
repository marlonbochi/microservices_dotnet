using MassTransit.Testing;
using Store.Contracts.Inventory;
using Store.Contracts.Ordering;
using Store.Testing;
using static Inventory.IntegrationTests.InventoryTestHelpers;

namespace Inventory.IntegrationTests;

/// <summary>Reservation messages processed end-to-end against a real SQL Server.</summary>
[Trait("Category", "Integration")]
public sealed class ReservationFlowTests(InventoryApiFactory factory) : IClassFixture<InventoryApiFactory>
{
    private const int ConcurrentOrders = 100;
    private const int UnitsInStock = 10;
    private static readonly TimeSpan ConcurrencyTimeout = TimeSpan.FromSeconds(90);

    private readonly HttpClient _client = factory.CreateClient();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ReserveThenRelease_RestoresAvailability()
    {
        var productId = await SeedProductAsync(factory, _client, initialStock: 10);
        var orderId = Guid.NewGuid();

        await factory.Harness.Bus.Publish(new ReserveStock(orderId, [new OrderLine(productId, 4)]), Token);
        (await factory.Harness.WaitForPublishedAsync<StockReserved>(message => message.OrderId == orderId)).ShouldBeTrue();
        (await GetStockAsync(_client, productId)).Available.ShouldBe(6);

        await factory.Harness.Bus.Publish(new ReleaseStock(orderId), Token);
        (await factory.Harness.WaitForPublishedAsync<StockReleased>(message => message.OrderId == orderId)).ShouldBeTrue();

        var stock = await GetStockAsync(_client, productId);
        stock.Available.ShouldBe(10);
        stock.QuantityOnHand.ShouldBe(10);
    }

    [Fact]
    public async Task ReserveThenCommit_DeductsOnHand()
    {
        var productId = await SeedProductAsync(factory, _client, initialStock: 10);
        var orderId = Guid.NewGuid();

        await factory.Harness.Bus.Publish(new ReserveStock(orderId, [new OrderLine(productId, 3)]), Token);
        (await factory.Harness.WaitForPublishedAsync<StockReserved>(message => message.OrderId == orderId)).ShouldBeTrue();
        await factory.Harness.Bus.Publish(new CommitStock(orderId), Token);
        (await factory.Harness.WaitForPublishedAsync<StockCommitted>(message => message.OrderId == orderId)).ShouldBeTrue();

        var stock = await GetStockAsync(_client, productId);
        stock.QuantityOnHand.ShouldBe(7);
        stock.QuantityReserved.ShouldBe(0);
    }

    /// <summary>SC-004: 100 concurrent orders for the last 10 units never oversell.</summary>
    [Fact]
    public async Task ConcurrentReservations_NeverOversell()
    {
        var productId = await SeedProductAsync(factory, _client, UnitsInStock);
        var orderIds = Enumerable.Range(0, ConcurrentOrders).Select(_ => Guid.NewGuid()).ToHashSet();

        await Task.WhenAll(orderIds.Select(orderId =>
            factory.Harness.Bus.Publish(new ReserveStock(orderId, [new OrderLine(productId, 1)]), Token)));

        var (reserved, failed) = await Eventually.Get(
            () => Task.FromResult(CountOutcomes(orderIds)),
            outcome => outcome.Reserved + outcome.Failed == ConcurrentOrders,
            ConcurrencyTimeout);

        reserved.ShouldBe(UnitsInStock);
        failed.ShouldBe(ConcurrentOrders - UnitsInStock);
        var stock = await GetStockAsync(_client, productId);
        stock.QuantityReserved.ShouldBe(UnitsInStock);
        stock.Available.ShouldBe(0);
    }

    private (int Reserved, int Failed) CountOutcomes(HashSet<Guid> orderIds)
    {
        var reserved = factory.Harness.CountPublished<StockReserved>(message => orderIds.Contains(message.OrderId));
        var failed = factory.Harness.CountPublished<StockReservationFailed>(message => orderIds.Contains(message.OrderId));
        return (reserved, failed);
    }
}
