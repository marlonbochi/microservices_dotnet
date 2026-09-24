using MassTransit;
using Ordering.Application.Abstractions;

namespace Ordering.Infrastructure.Messaging;

/// <summary>Publishes through the MassTransit bus outbox (written to the DB, sent after commit).</summary>
internal sealed class OutboxEventPublisher(IPublishEndpoint publishEndpoint) : IIntegrationEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : class =>
        publishEndpoint.Publish(integrationEvent, cancellationToken);
}
