namespace Store.Contracts.Payment;

/// <summary>Command: charge the order total.</summary>
public sealed record ProcessPayment(Guid OrderId, decimal Amount, bool SimulateFailure);

public sealed record PaymentApproved(Guid OrderId, string TransactionId);

public sealed record PaymentDeclined(Guid OrderId, string Reason);
