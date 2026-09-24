using MassTransit;
using Microsoft.Extensions.Logging;
using Ordering.Application.Abstractions;
using Ordering.Domain.Orders;
using Store.SharedKernel;

namespace Ordering.Infrastructure.Messaging.Sagas;

/// <summary>
/// Mirrors the saga state on the Order aggregate. It runs inside the saga transaction and uses the same
/// DbContext, so the order change is committed together with the saga state and the outgoing messages.
/// </summary>
public sealed partial class SyncOrderStatusActivity(
    IOrderRepository orders,
    TimeProvider timeProvider,
    ILogger<SyncOrderStatusActivity> logger) : IStateMachineActivity<OrderState>
{
    private static readonly Dictionary<string, Func<Order, OrderState, DateTimeOffset, Result>> Transitions = new()
    {
        [nameof(OrderStateMachine.AwaitingPayment)] = (order, _, now) => order.MarkStockReserved(now),
        [nameof(OrderStateMachine.Rejected)] = (order, saga, now) => order.Reject(saga.FailureReason ?? string.Empty, now),
        [nameof(OrderStateMachine.AwaitingCommit)] = (order, _, now) => order.MarkPaymentApproved(now),
        [nameof(OrderStateMachine.AwaitingRelease)] = (order, saga, now) => order.MarkPaymentDeclined(saga.FailureReason ?? string.Empty, now),
        [nameof(OrderStateMachine.Confirmed)] = (order, _, now) => order.Confirm(now),
        [nameof(OrderStateMachine.Cancelled)] = (order, _, now) => order.Cancel(now),
    };

    public void Probe(ProbeContext context) => context.CreateScope("sync-order-status");

    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

    public async Task Execute(BehaviorContext<OrderState> context, IBehavior<OrderState> next)
    {
        await SyncAsync(context.Saga, context.CancellationToken);
        await next.Execute(context);
    }

    public async Task Execute<T>(BehaviorContext<OrderState, T> context, IBehavior<OrderState, T> next)
        where T : class
    {
        await SyncAsync(context.Saga, context.CancellationToken);
        await next.Execute(context);
    }

    public Task Faulted<TException>(BehaviorExceptionContext<OrderState, TException> context, IBehavior<OrderState> next)
        where TException : Exception => next.Faulted(context);

    public Task Faulted<T, TException>(BehaviorExceptionContext<OrderState, T, TException> context, IBehavior<OrderState, T> next)
        where T : class
        where TException : Exception => next.Faulted(context);

    private async Task SyncAsync(OrderState saga, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(saga.CorrelationId, cancellationToken)
            ?? throw new InvalidOperationException($"Order {saga.CorrelationId} not found for saga state {saga.CurrentState}.");

        if (!Transitions.TryGetValue(saga.CurrentState, out var transition))
        {
            return;
        }

        var result = transition(order, saga, timeProvider.GetUtcNow());
        if (result.IsFailure)
        {
            LogTransitionRejected(logger, order.Id, saga.CurrentState, result.Error!.Message);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Order {OrderId} rejected saga state {State}: {Reason}")]
    private static partial void LogTransitionRejected(ILogger logger, Guid orderId, string state, string reason);
}
