using MassTransit;

namespace Ordering.Infrastructure.Messaging.Sagas;

/// <summary>Persistent state of one running order process (one row per order).</summary>
public sealed class OrderState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }

    public string CurrentState { get; set; } = string.Empty;

    public decimal Total { get; set; }

    public bool SimulatePaymentFailure { get; set; }

    public string? FailureReason { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
