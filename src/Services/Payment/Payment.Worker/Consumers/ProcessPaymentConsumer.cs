using MassTransit;
using Payment.Worker.Gateway;
using Store.Contracts.Payment;

namespace Payment.Worker.Consumers;

/// <summary>Charges the order and reports the outcome back to the order saga.</summary>
public sealed class ProcessPaymentConsumer(IPaymentGateway gateway) : IConsumer<ProcessPayment>
{
    public async Task Consume(ConsumeContext<ProcessPayment> context)
    {
        var message = context.Message;
        var result = await gateway.ChargeAsync(
            new PaymentRequest(message.OrderId, message.Amount, message.SimulateFailure),
            context.CancellationToken);

        if (result.Approved)
        {
            await context.Publish(new PaymentApproved(message.OrderId, result.TransactionId!));
            return;
        }

        await context.Publish(new PaymentDeclined(message.OrderId, result.DeclineReason!));
    }
}
