using Inventory.Application.Abstractions;
using Inventory.Domain.Reservations;
using Inventory.Domain.Stock;
using Store.Contracts.Inventory;
using Store.SharedKernel;

namespace Inventory.Application.Reservations;

public sealed record ReserveStockCommand(Guid OrderId, IReadOnlyList<ReservationLine> Lines);

/// <summary>
/// Reserves every line of an order or none of them (all-or-nothing) and answers the saga with
/// <see cref="StockReserved"/> or <see cref="StockReservationFailed"/>.
/// Concurrent reservations are protected by optimistic concurrency on <see cref="StockItem"/>:
/// the loser gets a concurrency exception and the message is retried with fresh data.
/// </summary>
public sealed class ReserveStockHandler(
    IStockItemRepository stockItems,
    IStockReservationRepository reservations,
    IIntegrationEventPublisher publisher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ICommandHandler<ReserveStockCommand>
{
    public async Task<Result> HandleAsync(ReserveStockCommand command, CancellationToken cancellationToken = default)
    {
        if (await reservations.GetAsync(command.OrderId, cancellationToken) is not null)
        {
            // Duplicate command: the reservation already exists, just acknowledge again.
            await publisher.PublishAsync(new StockReserved(command.OrderId), cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        var lines = MergeDuplicateProducts(command.Lines);
        var reserved = await TryReserveAllAsync(lines, cancellationToken);
        if (reserved.IsFailure)
        {
            await publisher.PublishAsync(new StockReservationFailed(command.OrderId, reserved.Error!.Message), cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return reserved;
        }

        reservations.Add(StockReservation.Create(command.OrderId, lines, timeProvider.GetUtcNow()));
        await publisher.PublishAsync(new StockReserved(command.OrderId), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Result> TryReserveAllAsync(IReadOnlyList<ReservationLine> lines, CancellationToken cancellationToken)
    {
        var items = (await stockItems.GetManyAsync([.. lines.Select(line => line.ProductId)], cancellationToken))
            .ToDictionary(item => item.Id);

        // Validate everything first so a failure leaves no partial reservation behind.
        foreach (var line in lines)
        {
            if (!items.TryGetValue(line.ProductId, out var item))
            {
                return StockErrors.UnknownProduct(line.ProductId);
            }

            if (line.Quantity > item.Available)
            {
                return StockErrors.Insufficient(item.Sku, line.Quantity, item.Available);
            }
        }

        foreach (var line in lines)
        {
            var result = items[line.ProductId].Reserve(line.Quantity);
            if (result.IsFailure)
            {
                return result;
            }
        }

        return Result.Success();
    }

    private static List<ReservationLine> MergeDuplicateProducts(IEnumerable<ReservationLine> lines) =>
        [.. lines.GroupBy(line => line.ProductId).Select(group => new ReservationLine(group.Key, group.Sum(line => line.Quantity)))];
}
