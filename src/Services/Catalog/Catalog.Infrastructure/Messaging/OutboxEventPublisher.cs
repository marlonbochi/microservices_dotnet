using Catalog.Application.Abstractions;
using MassTransit;

namespace Catalog.Infrastructure.Messaging;

/// <summary>
/// Adapter from the application port to MassTransit. Because the bus outbox is enabled, the scoped
/// <see cref="IPublishEndpoint"/> writes to the OutboxMessage table instead of RabbitMQ; the message
/// is delivered by a background service only after SaveChanges commits.
/// </summary>
internal sealed class OutboxEventPublisher(IPublishEndpoint publishEndpoint) : IIntegrationEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : class =>
        publishEndpoint.Publish(integrationEvent, cancellationToken);
}
