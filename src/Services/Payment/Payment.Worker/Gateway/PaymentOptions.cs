namespace Payment.Worker.Gateway;

public sealed class PaymentOptions
{
    public const string SectionName = "Payment";

    /// <summary>Artificial latency so the order timeline is visible in the UI.</summary>
    public TimeSpan ProcessingDelay { get; init; } = TimeSpan.FromSeconds(1.5);

    /// <summary>Orders above this amount are declined ("card limit exceeded").</summary>
    public decimal CardLimit { get; init; } = 50_000m;
}
