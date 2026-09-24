using Microsoft.EntityFrameworkCore;
using Ordering.Application.Abstractions;
using Ordering.Domain.Orders;

namespace Ordering.Infrastructure.Persistence;

internal sealed class OrderRepository(OrderingDbContext dbContext) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Set<Order>().FirstOrDefaultAsync(order => order.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Order>> ListRecentAsync(int take, CancellationToken cancellationToken) =>
        await dbContext.Set<Order>()
            .AsNoTracking()
            .AsSplitQuery()
            .OrderByDescending(order => order.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public void Add(Order order) => dbContext.Add(order);
}
