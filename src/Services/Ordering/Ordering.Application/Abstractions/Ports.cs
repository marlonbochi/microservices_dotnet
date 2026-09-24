using Ordering.Domain.Orders;
using Store.SharedKernel;

namespace Ordering.Application.Abstractions;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Order>> ListRecentAsync(int take, CancellationToken cancellationToken);

    void Add(Order order);
}

/// <summary>Snapshot of a catalog product as seen by Ordering.</summary>
public sealed record CatalogProduct(Guid Id, string Name, decimal Price);

/// <summary>Synchronous query port to the Catalog service (fresh, authoritative prices).</summary>
public interface ICatalogClient
{
    /// <returns>The products found, or an <see cref="ErrorType.Unavailable"/> error if Catalog cannot be reached.</returns>
    Task<Result<IReadOnlyList<CatalogProduct>>> GetProductsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
}

public interface IIntegrationEventPublisher
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : class;
}

public sealed record OrderStatusNotification(Guid OrderId, OrderStatus Status, string? FailureReason, DateTimeOffset OccurredAt);

/// <summary>Pushes order changes to connected clients (real-time UI).</summary>
public interface IOrderNotifier
{
    Task NotifyStatusChangedAsync(OrderStatusNotification notification, CancellationToken cancellationToken);
}
