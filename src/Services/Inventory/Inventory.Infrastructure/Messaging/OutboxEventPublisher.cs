using Inventory.Application.Abstractions;
using MassTransit;

namespace Inventory.Infrastructure.Messaging;

/// <summary>
/// Inside a consumer the scoped <see cref="IPublishEndpoint"/> is the ConsumeContext, and with the
/// consumer outbox enabled the message is stored and only sent after the DbContext commits.
/// </summary>
internal sealed class OutboxEventPublisher(IPublishEndpoint publishEndpoint) : IIntegrationEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : class =>
        publishEndpoint.Publish(integrationEvent, cancellationToken);
}
