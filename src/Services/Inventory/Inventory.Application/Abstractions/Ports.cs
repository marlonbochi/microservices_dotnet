using Inventory.Domain.Reservations;
using Inventory.Domain.Stock;

namespace Inventory.Application.Abstractions;

public interface IStockItemRepository
{
    Task<StockItem?> GetAsync(Guid productId, CancellationToken cancellationToken);

    Task<IReadOnlyList<StockItem>> GetManyAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken);

    Task<IReadOnlyList<StockItem>> ListAsync(CancellationToken cancellationToken);

    void Add(StockItem stockItem);
}

public interface IStockReservationRepository
{
    Task<StockReservation?> GetAsync(Guid orderId, CancellationToken cancellationToken);

    void Add(StockReservation reservation);
}

/// <summary>Publishes integration events through the transactional outbox.</summary>
public interface IIntegrationEventPublisher
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : class;
}
