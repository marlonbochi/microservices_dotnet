using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Ordering.Application.Abstractions;
using Ordering.Domain.Orders;
using Ordering.Infrastructure.Messaging.Sagas;
using Ordering.UnitTests.Fakes;
using Store.Contracts.Inventory;
using Store.Contracts.Ordering;
using Store.Contracts.Payment;

namespace Ordering.UnitTests.Sagas;

/// <summary>Drives the order saga through every transition with the in-memory MassTransit harness.</summary>
public sealed class OrderStateMachineTests : IAsyncLifetime
{
    private static readonly TimeSpan StatusTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan StatusPollInterval = TimeSpan.FromMilliseconds(20);

    private readonly InMemoryOrders _orders = new();
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;
    private ISagaStateMachineTestHarness<OrderStateMachine, OrderState> _saga = null!;
    private OrderStateMachine _machine = null!;

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _provider = new ServiceCollection()
            .AddSingleton<IOrderRepository>(_orders)
            .AddSingleton(TimeProvider.System)
            .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
            .AddMassTransitTestHarness(bus => bus.AddSagaStateMachine<OrderStateMachine, OrderState>().InMemoryRepository())
            .BuildServiceProvider(validateScopes: true);
        _harness = _provider.GetTestHarness();
        await _harness.Start();
        _saga = _harness.GetSagaStateMachineHarness<OrderStateMachine, OrderState>();
        _machine = _saga.StateMachine;
    }

    public async ValueTask DisposeAsync() => await _provider.DisposeAsync();

    /// <summary>
    /// The harness reports the new saga state as soon as TransitionTo runs, but the activity that mirrors
    /// it on the Order executes right after, so wait briefly instead of asserting immediately.
    /// </summary>
    private static async Task ShouldReachStatusAsync(Order order, OrderStatus expected)
    {
        var deadline = DateTime.UtcNow + StatusTimeout;
        while (order.Status != expected && DateTime.UtcNow < deadline)
        {
            await Task.Delay(StatusPollInterval, Token);
        }

        order.Status.ShouldBe(expected);
    }

    private async Task<Order> SubmitOrderAsync(bool simulatePaymentFailure = false)
    {
        var order = Order.Place(
            "Ana", "ana@example.com", [new OrderItem(Guid.NewGuid(), "Teclado", 100m, 2)], simulatePaymentFailure, DateTimeOffset.UtcNow).Value;
        _orders.Add(order);

        await _harness.Bus.Publish(
            new OrderSubmitted(order.Id, order.CustomerEmail, order.Total, simulatePaymentFailure,
                [.. order.Items.Select(item => new OrderLine(item.ProductId, item.Quantity))], order.CreatedAt),
            Token);
        (await _saga.Exists(order.Id, _machine.AwaitingStock)).ShouldNotBeNull();
        return order;
    }

    [Fact]
    public async Task OrderSubmitted_CreatesSagaAndRequestsStockReservation()
    {
        var order = await SubmitOrderAsync();

        (await _harness.Published.Any<ReserveStock>(message => message.Context.Message.OrderId == order.Id, Token)).ShouldBeTrue();
    }

    [Fact]
    public async Task HappyPath_ReservesPaysCommitsAndConfirmsOrder()
    {
        var order = await SubmitOrderAsync();

        await _harness.Bus.Publish(new StockReserved(order.Id), Token);
        (await _saga.Exists(order.Id, _machine.AwaitingPayment)).ShouldNotBeNull();
        (await _harness.Published.Any<ProcessPayment>(message => message.Context.Message.Amount == 200m, Token)).ShouldBeTrue();

        await _harness.Bus.Publish(new PaymentApproved(order.Id, "TX-1"), Token);
        (await _saga.Exists(order.Id, _machine.AwaitingCommit)).ShouldNotBeNull();
        (await _harness.Published.Any<CommitStock>(message => message.Context.Message.OrderId == order.Id, Token)).ShouldBeTrue();

        await _harness.Bus.Publish(new StockCommitted(order.Id), Token);
        (await _saga.Exists(order.Id, _machine.Confirmed)).ShouldNotBeNull();

        await ShouldReachStatusAsync(order, OrderStatus.Confirmed);
        order.History.Select(change => change.Status).ShouldBe(
            [OrderStatus.Submitted, OrderStatus.StockReserved, OrderStatus.PaymentApproved, OrderStatus.Confirmed]);
    }

    [Fact]
    public async Task StockReservationFailed_RejectsOrderWithReason()
    {
        var order = await SubmitOrderAsync();

        await _harness.Bus.Publish(new StockReservationFailed(order.Id, "Insufficient stock"), Token);

        (await _saga.Exists(order.Id, _machine.Rejected)).ShouldNotBeNull();
        await ShouldReachStatusAsync(order, OrderStatus.Rejected);
        order.FailureReason.ShouldBe("Insufficient stock");
        (await _harness.Published.Any<ProcessPayment>(Token)).ShouldBeFalse();
    }

    [Fact]
    public async Task PaymentDeclined_CompensatesByReleasingStockThenCancels()
    {
        var order = await SubmitOrderAsync(simulatePaymentFailure: true);
        await _harness.Bus.Publish(new StockReserved(order.Id), Token);
        (await _saga.Exists(order.Id, _machine.AwaitingPayment)).ShouldNotBeNull();

        await _harness.Bus.Publish(new PaymentDeclined(order.Id, "Card declined"), Token);
        (await _saga.Exists(order.Id, _machine.AwaitingRelease)).ShouldNotBeNull();
        (await _harness.Published.Any<ReleaseStock>(message => message.Context.Message.OrderId == order.Id, Token)).ShouldBeTrue();
        await ShouldReachStatusAsync(order, OrderStatus.PaymentDeclined);

        await _harness.Bus.Publish(new StockReleased(order.Id), Token);
        (await _saga.Exists(order.Id, _machine.Cancelled)).ShouldNotBeNull();

        await ShouldReachStatusAsync(order, OrderStatus.Cancelled);
        order.FailureReason.ShouldBe("Card declined");
        (await _harness.Published.Any<CommitStock>(Token)).ShouldBeFalse();
    }

    [Fact]
    public async Task DuplicateStockReserved_IsIgnored()
    {
        var order = await SubmitOrderAsync();
        await _harness.Bus.Publish(new StockReserved(order.Id), Token);
        (await _saga.Exists(order.Id, _machine.AwaitingPayment)).ShouldNotBeNull();

        await _harness.Bus.Publish(new StockReserved(order.Id), Token);
        await _harness.InactivityTask;

        (await _saga.Exists(order.Id, _machine.AwaitingPayment)).ShouldNotBeNull();
        _harness.Published.Select<ProcessPayment>(Token).Count().ShouldBe(1);
        (await _harness.Published.Any<Fault<StockReserved>>(Token)).ShouldBeFalse();
    }

    [Fact]
    public async Task EventForUnknownOrder_IsDiscarded()
    {
        await _harness.Bus.Publish(new StockReserved(Guid.NewGuid()), Token);

        (await _harness.Consumed.Any<StockReserved>(Token)).ShouldBeTrue();
        (await _harness.Published.Any<Fault<StockReserved>>(Token)).ShouldBeFalse();
    }
}
