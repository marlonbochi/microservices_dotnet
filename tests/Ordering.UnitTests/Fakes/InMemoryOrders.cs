using System.Collections.Concurrent;
using Ordering.Application.Abstractions;
using Ordering.Domain.Orders;
using Store.SharedKernel;

namespace Ordering.UnitTests.Fakes;

internal sealed class InMemoryOrders : IOrderRepository, IIntegrationEventPublisher, IUnitOfWork
{
    private readonly ConcurrentDictionary<Guid, Order> _orders = new();

    public List<object> Published { get; } = [];

    public int Commits { get; private set; }

    public Order? Find(Guid id) => _orders.GetValueOrDefault(id);

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Find(id));

    public Task<IReadOnlyList<Order>> ListRecentAsync(int take, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Order>>([.. _orders.Values.OrderByDescending(order => order.CreatedAt).Take(take)]);

    public void Add(Order order) => _orders[order.Id] = order;

    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : class
    {
        Published.Add(integrationEvent);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        Commits++;
        return Task.CompletedTask;
    }
}
