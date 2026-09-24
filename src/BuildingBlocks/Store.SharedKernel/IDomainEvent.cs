namespace Store.SharedKernel;

/// <summary>Something meaningful that happened inside a bounded context.</summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}

public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}

/// <summary>Reacts to a domain event after the aggregate that raised it has been saved.</summary>
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}
