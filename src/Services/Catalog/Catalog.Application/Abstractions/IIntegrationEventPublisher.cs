namespace Catalog.Application.Abstractions;

/// <summary>
/// Port to publish integration events to other services. The implementation stores the message in
/// the transactional outbox, so it is only sent if the surrounding unit of work commits.
/// </summary>
public interface IIntegrationEventPublisher
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : class;
}
