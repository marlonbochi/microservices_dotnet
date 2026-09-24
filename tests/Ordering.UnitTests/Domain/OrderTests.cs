using Ordering.Domain.Orders;

namespace Ordering.UnitTests.Domain;

public sealed class OrderTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Keyboard = Guid.NewGuid();
    private static readonly Guid Mouse = Guid.NewGuid();

    private static Order NewOrder() => Order.Place(
        "Ana", "ANA@Example.com", [new OrderItem(Keyboard, "Teclado", 350m, 2)], simulatePaymentFailure: false, Now).Value;

    [Fact]
    public void Place_WithItems_ComputesTotalAndStartsSubmitted()
    {
        var result = Order.Place(
            "Ana", "ANA@Example.com",
            [new OrderItem(Keyboard, "Teclado", 350m, 2), new OrderItem(Mouse, "Mouse", 99.90m, 1)],
            simulatePaymentFailure: false, Now);

        var order = result.Value;
        order.Total.ShouldBe(799.90m);
        order.Status.ShouldBe(OrderStatus.Submitted);
        order.CustomerEmail.ShouldBe("ana@example.com");
        order.History.ShouldHaveSingleItem().Status.ShouldBe(OrderStatus.Submitted);
        order.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<OrderStatusChangedDomainEvent>();
    }

    [Fact]
    public void Place_WithSameProductTwice_MergesQuantities()
    {
        var order = Order.Place(
            "Ana", "ana@example.com",
            [new OrderItem(Keyboard, "Teclado", 10m, 1), new OrderItem(Keyboard, "Teclado", 10m, 2)],
            simulatePaymentFailure: false, Now).Value;

        order.Items.ShouldHaveSingleItem().Quantity.ShouldBe(3);
        order.Total.ShouldBe(30m);
    }

    [Fact]
    public void Place_WithoutItems_ReturnsNoItems()
    {
        Order.Place("Ana", "ana@example.com", [], simulatePaymentFailure: false, Now).Error.ShouldBe(OrderErrors.NoItems);
    }

    [Fact]
    public void Place_WithZeroQuantity_ReturnsInvalidQuantity()
    {
        Order.Place("Ana", "ana@example.com", [new OrderItem(Keyboard, "Teclado", 10m, 0)], simulatePaymentFailure: false, Now)
            .Error.ShouldBe(OrderErrors.InvalidQuantity);
    }

    [Fact]
    public void HappyPath_ReachesConfirmedWithFullTimeline()
    {
        var order = NewOrder();

        order.MarkStockReserved(Now).IsSuccess.ShouldBeTrue();
        order.MarkPaymentApproved(Now).IsSuccess.ShouldBeTrue();
        order.Confirm(Now).IsSuccess.ShouldBeTrue();

        order.Status.ShouldBe(OrderStatus.Confirmed);
        order.IsFinal.ShouldBeTrue();
        order.History.Select(change => change.Status).ShouldBe(
            [OrderStatus.Submitted, OrderStatus.StockReserved, OrderStatus.PaymentApproved, OrderStatus.Confirmed]);
    }

    [Fact]
    public void PaymentDeclined_ThenCancel_KeepsFailureReason()
    {
        var order = NewOrder();
        order.MarkStockReserved(Now);

        order.MarkPaymentDeclined("card declined", Now);
        order.Cancel(Now);

        order.Status.ShouldBe(OrderStatus.Cancelled);
        order.FailureReason.ShouldBe("card declined");
    }

    [Fact]
    public void Reject_FromSubmitted_SetsReason()
    {
        var order = NewOrder();

        order.Reject("insufficient stock", Now).IsSuccess.ShouldBeTrue();

        order.Status.ShouldBe(OrderStatus.Rejected);
        order.FailureReason.ShouldBe("insufficient stock");
    }

    [Fact]
    public void InvalidTransition_ReturnsErrorAndKeepsStatus()
    {
        var order = NewOrder();

        var result = order.Confirm(Now);

        result.Error!.Code.ShouldBe("Order.InvalidTransition");
        order.Status.ShouldBe(OrderStatus.Submitted);
    }

    [Fact]
    public void SameTransitionTwice_IsIdempotent()
    {
        var order = NewOrder();

        order.MarkStockReserved(Now);
        order.MarkStockReserved(Now).IsSuccess.ShouldBeTrue();

        order.History.Count.ShouldBe(2);
    }
}
