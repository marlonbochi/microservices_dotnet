using MassTransit;
using Microsoft.EntityFrameworkCore;
using Ordering.Infrastructure.DomainEvents;
using Ordering.Infrastructure.Messaging.Sagas;
using Store.SharedKernel;

namespace Ordering.Infrastructure.Persistence;

/// <summary>
/// Holds the Order aggregate, the saga state (OrderState) and the outbox/inbox tables in ONE database,
/// so a saga transition, the order status change and the outgoing messages commit atomically.
/// </summary>
public sealed class OrderingDbContext(
    DbContextOptions<OrderingDbContext> options,
    IDomainEventDispatcher domainEventDispatcher) : DbContext(options), IUnitOfWork
{
    public const string Schema = "ordering";

    async Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) =>
        await SaveChangesAsync(cancellationToken);

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var domainEvents = CollectDomainEvents();
        var affected = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

        // Dispatched only after the changes were written, so handlers never observe unsaved state.
        await domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);
        return affected;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderingDbContext).Assembly);
        new OrderStateMap().Configure(modelBuilder);

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }

    private List<IDomainEvent> CollectDomainEvents()
    {
        var aggregates = ChangeTracker.Entries<IHasDomainEvents>().Select(entry => entry.Entity).ToList();
        var domainEvents = aggregates.SelectMany(aggregate => aggregate.DomainEvents).ToList();
        aggregates.ForEach(aggregate => aggregate.ClearDomainEvents());
        return domainEvents;
    }
}
