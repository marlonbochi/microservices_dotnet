namespace Payment.Worker.Gateway;

public sealed record PaymentRequest(Guid OrderId, decimal Amount, bool SimulateFailure);

public sealed record PaymentResult(bool Approved, string? TransactionId, string? DeclineReason)
{
    public static PaymentResult Approve(string transactionId) => new(true, transactionId, null);

    public static PaymentResult Decline(string reason) => new(false, null, reason);
}

/// <summary>Port to a payment provider. The demo uses <see cref="FakePaymentGateway"/>.</summary>
public interface IPaymentGateway
{
    Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken cancellationToken);
}
