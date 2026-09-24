using Inventory.Application.Abstractions;
using Inventory.Domain.Reservations;
using Inventory.Domain.Stock;
using Store.SharedKernel;

namespace Inventory.UnitTests.Fakes;

/// <summary>In-memory adapters for the application ports (Liskov: they honour the same contracts).</summary>
internal sealed class InMemoryInventory : IStockItemRepository, IStockReservationRepository, IIntegrationEventPublisher, IUnitOfWork
{
    private readonly Dictionary<Guid, StockItem> _items = [];
    private readonly Dictionary<Guid, StockReservation> _reservations = [];
    private readonly List<object> _pending = [];

    public List<object> Published { get; } = [];

    public int Commits { get; private set; }

    public StockItem Seed(int onHand, string sku = "SKU-1")
    {
        var item = StockItem.Register(Guid.NewGuid(), sku, $"Product {sku}", onHand).Value;
        _items[item.Id] = item;
        return item;
    }

    public StockReservation? Reservation(Guid orderId) => _reservations.GetValueOrDefault(orderId);

    Task<StockItem?> IStockItemRepository.GetAsync(Guid productId, CancellationToken cancellationToken) =>
        Task.FromResult(_items.GetValueOrDefault(productId));

    public Task<IReadOnlyList<StockItem>> GetManyAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<StockItem>>([.. _items.Values.Where(item => productIds.Contains(item.Id))]);

    public Task<IReadOnlyList<StockItem>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<StockItem>>([.. _items.Values]);

    public void Add(StockItem stockItem) => _items[stockItem.Id] = stockItem;

    Task<StockReservation?> IStockReservationRepository.GetAsync(Guid orderId, CancellationToken cancellationToken) =>
        Task.FromResult(_reservations.GetValueOrDefault(orderId));

    public void Add(StockReservation reservation) => _reservations[reservation.Id] = reservation;

    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : class
    {
        _pending.Add(integrationEvent);
        return Task.CompletedTask;
    }

    /// <summary>Like the outbox: messages only become "published" when the unit of work commits.</summary>
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        Commits++;
        Published.AddRange(_pending);
        _pending.Clear();
        return Task.CompletedTask;
    }
}
