using MassTransit;

namespace Store.Testing;

/// <summary>
/// Plays the role of a downstream service: consuming the message proves it really left the service
/// through the broker (outbox included). Assert on it with <see cref="HarnessExtensions"/>.
/// </summary>
public sealed class MessageCapture<T> : IConsumer<T>
    where T : class
{
    public Task Consume(ConsumeContext<T> context) => Task.CompletedTask;
}
