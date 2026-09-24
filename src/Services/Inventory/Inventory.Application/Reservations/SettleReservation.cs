using Inventory.Application.Abstractions;
using Inventory.Domain.Reservations;
using Inventory.Domain.Stock;
using Microsoft.Extensions.Logging;
using Store.Contracts.Inventory;
using Store.SharedKernel;

namespace Inventory.Application.Reservations;

public sealed record CommitStockCommand(Guid OrderId);

public sealed record ReleaseStockCommand(Guid OrderId);

/// <summary>Payment approved: the reserved units are definitively deducted.</summary>
public sealed class CommitStockHandler(
    IStockItemRepository stockItems,
    IStockReservationRepository reservations,
    IIntegrationEventPublisher publisher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<CommitStockHandler> logger) : ICommandHandler<CommitStockCommand>
{
    public Task<Result> HandleAsync(CommitStockCommand command, CancellationToken cancellationToken = default) =>
        ReservationSettlement.SettleAsync(
            new ReservationSettlement.Context(stockItems, reservations, publisher, unitOfWork, logger),
            command.OrderId,
            reservation => reservation.Commit(timeProvider.GetUtcNow()),
            (item, quantity) => item.CommitReservation(quantity),
            new StockCommitted(command.OrderId),
            cancellationToken);
}

/// <summary>Compensation for a declined payment: the reserved units become available again.</summary>
public sealed class ReleaseStockHandler(
    IStockItemRepository stockItems,
    IStockReservationRepository reservations,
    IIntegrationEventPublisher publisher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<ReleaseStockHandler> logger) : ICommandHandler<ReleaseStockCommand>
{
    public Task<Result> HandleAsync(ReleaseStockCommand command, CancellationToken cancellationToken = default) =>
        ReservationSettlement.SettleAsync(
            new ReservationSettlement.Context(stockItems, reservations, publisher, unitOfWork, logger),
            command.OrderId,
            reservation => reservation.Release(timeProvider.GetUtcNow()),
            (item, quantity) => item.ReleaseReservation(quantity),
            new StockReleased(command.OrderId),
            cancellationToken);
}

/// <summary>Shared algorithm for commit and release (they differ only in the domain operations).</summary>
internal static partial class ReservationSettlement
{
    internal sealed record Context(
        IStockItemRepository StockItems,
        IStockReservationRepository Reservations,
        IIntegrationEventPublisher Publisher,
        IUnitOfWork UnitOfWork,
        ILogger Logger);

    public static async Task<Result> SettleAsync<TEvent>(
        Context context,
        Guid orderId,
        Func<StockReservation, bool> settleReservation,
        Action<StockItem, int> settleStock,
        TEvent acknowledgement,
        CancellationToken cancellationToken)
        where TEvent : class
    {
        var reservation = await context.Reservations.GetAsync(orderId, cancellationToken);
        if (reservation is null)
        {
            LogReservationNotFound(context.Logger, orderId);
            return Error.NotFound("Reservation.NotFound", $"No reservation for order '{orderId}'.");
        }

        if (settleReservation(reservation))
        {
            var items = (await context.StockItems.GetManyAsync([.. reservation.Lines.Select(line => line.ProductId)], cancellationToken))
                .ToDictionary(item => item.Id);
            foreach (var line in reservation.Lines)
            {
                settleStock(items[line.ProductId], line.Quantity);
            }
        }

        // Acknowledge even when it was a duplicate, so the saga never waits forever.
        await context.Publisher.PublishAsync(acknowledgement, cancellationToken);
        await context.UnitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "No reservation found for order {OrderId}")]
    private static partial void LogReservationNotFound(ILogger logger, Guid orderId);
}
