using Inventory.Application.Reservations;
using Inventory.Domain.Reservations;
using MassTransit;
using Store.Contracts.Inventory;
using Store.SharedKernel;

namespace Inventory.Infrastructure.Messaging.Consumers;

public sealed class ReserveStockConsumer(ICommandHandler<ReserveStockCommand> handler) : IConsumer<ReserveStock>
{
    public async Task Consume(ConsumeContext<ReserveStock> context)
    {
        var lines = context.Message.Items.Select(item => new ReservationLine(item.ProductId, item.Quantity)).ToList();

        // A failed reservation is a business outcome (StockReservationFailed is published), not an error.
        await handler.HandleAsync(new ReserveStockCommand(context.Message.OrderId, lines), context.CancellationToken);
    }
}

public sealed class CommitStockConsumer(ICommandHandler<CommitStockCommand> handler) : IConsumer<CommitStock>
{
    public async Task Consume(ConsumeContext<CommitStock> context) =>
        await handler.HandleAsync(new CommitStockCommand(context.Message.OrderId), context.CancellationToken);
}

public sealed class ReleaseStockConsumer(ICommandHandler<ReleaseStockCommand> handler) : IConsumer<ReleaseStock>
{
    public async Task Consume(ConsumeContext<ReleaseStock> context) =>
        await handler.HandleAsync(new ReleaseStockCommand(context.Message.OrderId), context.CancellationToken);
}
