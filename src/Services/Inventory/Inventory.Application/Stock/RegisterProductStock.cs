using Inventory.Application.Abstractions;
using Inventory.Domain.Stock;
using Microsoft.Extensions.Logging;
using Store.SharedKernel;

namespace Inventory.Application.Stock;

public sealed record RegisterProductStockCommand(Guid ProductId, string Sku, string ProductName, int InitialQuantity);

/// <summary>Reacts to a product created in Catalog by opening its stock record (idempotent).</summary>
public sealed partial class RegisterProductStockHandler(
    IStockItemRepository stockItems,
    IUnitOfWork unitOfWork,
    ILogger<RegisterProductStockHandler> logger) : ICommandHandler<RegisterProductStockCommand>
{
    public async Task<Result> HandleAsync(RegisterProductStockCommand command, CancellationToken cancellationToken = default)
    {
        if (await stockItems.GetAsync(command.ProductId, cancellationToken) is not null)
        {
            LogAlreadyRegistered(logger, command.ProductId);
            return Result.Success();
        }

        var registered = StockItem.Register(command.ProductId, command.Sku, command.ProductName, command.InitialQuantity);
        if (registered.IsFailure)
        {
            return registered;
        }

        stockItems.Add(registered.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Stock for product {ProductId} already registered; ignoring duplicate")]
    private static partial void LogAlreadyRegistered(ILogger logger, Guid productId);
}
