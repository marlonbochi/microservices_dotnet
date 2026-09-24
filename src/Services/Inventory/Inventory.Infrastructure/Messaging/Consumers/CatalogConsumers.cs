using Inventory.Application.Stock;
using MassTransit;
using Store.Contracts.Catalog;
using Store.SharedKernel;

namespace Inventory.Infrastructure.Messaging.Consumers;

/// <summary>Thin adapter: translates the integration event into an application command.</summary>
public sealed class ProductCreatedConsumer(ICommandHandler<RegisterProductStockCommand> handler)
    : IConsumer<ProductCreated>
{
    public async Task Consume(ConsumeContext<ProductCreated> context)
    {
        var message = context.Message;
        await handler.HandleAsync(
            new RegisterProductStockCommand(message.ProductId, message.Sku, message.Name, message.InitialStock),
            context.CancellationToken);
    }
}

public sealed class ProductUpdatedConsumer(ICommandHandler<RenameProductCommand> handler)
    : IConsumer<ProductUpdated>
{
    public async Task Consume(ConsumeContext<ProductUpdated> context)
    {
        var result = await handler.HandleAsync(
            new RenameProductCommand(context.Message.ProductId, context.Message.Name),
            context.CancellationToken);

        // The ProductCreated event may still be in flight: throw so the retry policy tries again later.
        if (result.Error?.Type == ErrorType.NotFound)
        {
            throw new InvalidOperationException(result.Error.Message);
        }
    }
}
