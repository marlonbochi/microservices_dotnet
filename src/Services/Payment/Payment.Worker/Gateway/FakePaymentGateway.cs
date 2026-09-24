using Microsoft.Extensions.Options;

namespace Payment.Worker.Gateway;

/// <summary>Simulated payment provider: approves unless asked to fail or the amount exceeds the card limit.</summary>
internal sealed class FakePaymentGateway(IOptions<PaymentOptions> options, TimeProvider timeProvider) : IPaymentGateway
{
    public const string SimulatedDeclineReason = "Payment declined by the card issuer (simulated failure).";

    public async Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken cancellationToken)
    {
        await Task.Delay(options.Value.ProcessingDelay, timeProvider, cancellationToken);

        if (request.SimulateFailure)
        {
            return PaymentResult.Decline(SimulatedDeclineReason);
        }

        if (request.Amount > options.Value.CardLimit)
        {
            return PaymentResult.Decline($"Amount {request.Amount:N2} exceeds the card limit of {options.Value.CardLimit:N2}.");
        }

        return PaymentResult.Approve($"TX-{Guid.NewGuid():N}"[..19].ToUpperInvariant());
    }
}
