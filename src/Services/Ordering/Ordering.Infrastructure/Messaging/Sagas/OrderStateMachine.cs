using MassTransit;
using Store.Contracts.Inventory;
using Store.Contracts.Ordering;
using Store.Contracts.Payment;

namespace Ordering.Infrastructure.Messaging.Sagas;

/// <summary>
/// Orchestrates the order process across Inventory and Payment (saga pattern).
///
///   Initial ──OrderSubmitted──▶ AwaitingStock ──StockReserved──▶ AwaitingPayment ──PaymentApproved──▶ AwaitingCommit ──StockCommitted──▶ Confirmed
///                                    │                                  │
///                          StockReservationFailed               PaymentDeclined (compensate: ReleaseStock)
///                                    ▼                                  ▼
///                                 Rejected                       AwaitingRelease ──StockReleased──▶ Cancelled
///
/// After each transition <see cref="SyncOrderStatusActivity"/> mirrors the new state on the Order aggregate.
/// </summary>
public sealed class OrderStateMachine : MassTransitStateMachine<OrderState>
{
    public OrderStateMachine()
    {
        InstanceState(state => state.CurrentState);
        ConfigureEvents();

        Initially(
            When(OrderSubmitted)
                .Then(context =>
                {
                    context.Saga.Total = context.Message.Total;
                    context.Saga.SimulatePaymentFailure = context.Message.SimulatePaymentFailure;
                    context.Saga.CreatedAt = context.Message.OccurredAt;
                })
                .TransitionTo(AwaitingStock)
                .Publish(context => new ReserveStock(context.Saga.CorrelationId, context.Message.Items)));

        During(AwaitingStock,
            When(StockReserved)
                .TransitionTo(AwaitingPayment)
                .Activity(sync => sync.OfInstanceType<SyncOrderStatusActivity>())
                .Publish(context => new ProcessPayment(context.Saga.CorrelationId, context.Saga.Total, context.Saga.SimulatePaymentFailure)),
            When(StockReservationFailed)
                .Then(context => context.Saga.FailureReason = context.Message.Reason)
                .TransitionTo(Rejected)
                .Activity(sync => sync.OfInstanceType<SyncOrderStatusActivity>()));

        During(AwaitingPayment,
            When(PaymentApproved)
                .TransitionTo(AwaitingCommit)
                .Activity(sync => sync.OfInstanceType<SyncOrderStatusActivity>())
                .Publish(context => new CommitStock(context.Saga.CorrelationId)),
            When(PaymentDeclined)
                .Then(context => context.Saga.FailureReason = context.Message.Reason)
                .TransitionTo(AwaitingRelease)
                .Activity(sync => sync.OfInstanceType<SyncOrderStatusActivity>())
                .Publish(context => new ReleaseStock(context.Saga.CorrelationId)));

        During(AwaitingCommit,
            When(StockCommitted)
                .TransitionTo(Confirmed)
                .Activity(sync => sync.OfInstanceType<SyncOrderStatusActivity>()));

        During(AwaitingRelease,
            When(StockReleased)
                .TransitionTo(Cancelled)
                .Activity(sync => sync.OfInstanceType<SyncOrderStatusActivity>()));

        // Duplicated or late messages (at-least-once delivery) must not break the process.
        OnUnhandledEvent(context => context.Ignore());
    }

    public State AwaitingStock { get; private set; } = null!;

    public State AwaitingPayment { get; private set; } = null!;

    public State AwaitingCommit { get; private set; } = null!;

    public State AwaitingRelease { get; private set; } = null!;

    public State Confirmed { get; private set; } = null!;

    public State Rejected { get; private set; } = null!;

    public State Cancelled { get; private set; } = null!;

    public Event<OrderSubmitted> OrderSubmitted { get; private set; } = null!;

    public Event<StockReserved> StockReserved { get; private set; } = null!;

    public Event<StockReservationFailed> StockReservationFailed { get; private set; } = null!;

    public Event<PaymentApproved> PaymentApproved { get; private set; } = null!;

    public Event<PaymentDeclined> PaymentDeclined { get; private set; } = null!;

    public Event<StockCommitted> StockCommitted { get; private set; } = null!;

    public Event<StockReleased> StockReleased { get; private set; } = null!;

    private void ConfigureEvents()
    {
        Event(() => OrderSubmitted, e => e.CorrelateById(context => context.Message.OrderId));
        Event(() => StockReserved, e => CorrelateExisting(e, context => context.Message.OrderId));
        Event(() => StockReservationFailed, e => CorrelateExisting(e, context => context.Message.OrderId));
        Event(() => PaymentApproved, e => CorrelateExisting(e, context => context.Message.OrderId));
        Event(() => PaymentDeclined, e => CorrelateExisting(e, context => context.Message.OrderId));
        Event(() => StockCommitted, e => CorrelateExisting(e, context => context.Message.OrderId));
        Event(() => StockReleased, e => CorrelateExisting(e, context => context.Message.OrderId));
    }

    private static void CorrelateExisting<TMessage>(
        IEventCorrelationConfigurator<OrderState, TMessage> configurator,
        Func<ConsumeContext<TMessage>, Guid> orderId)
        where TMessage : class
    {
        configurator.CorrelateById(orderId);
        configurator.OnMissingInstance(missing => missing.Discard());
    }
}
