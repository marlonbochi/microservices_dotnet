using Inventory.Application.Abstractions;
using Inventory.Domain.Reservations;
using Inventory.Domain.Stock;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

internal sealed class StockItemRepository(InventoryDbContext dbContext) : IStockItemRepository
{
    public Task<StockItem?> GetAsync(Guid productId, CancellationToken cancellationToken) =>
        dbContext.Set<StockItem>().FirstOrDefaultAsync(item => item.Id == productId, cancellationToken);

    public async Task<IReadOnlyList<StockItem>> GetManyAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken) =>
        await dbContext.Set<StockItem>().Where(item => productIds.Contains(item.Id)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<StockItem>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.Set<StockItem>().AsNoTracking().OrderBy(item => item.ProductName).ToListAsync(cancellationToken);

    public void Add(StockItem stockItem) => dbContext.Add(stockItem);
}

internal sealed class StockReservationRepository(InventoryDbContext dbContext) : IStockReservationRepository
{
    public Task<StockReservation?> GetAsync(Guid orderId, CancellationToken cancellationToken) =>
        dbContext.Set<StockReservation>().FirstOrDefaultAsync(reservation => reservation.Id == orderId, cancellationToken);

    public void Add(StockReservation reservation) => dbContext.Add(reservation);
}
