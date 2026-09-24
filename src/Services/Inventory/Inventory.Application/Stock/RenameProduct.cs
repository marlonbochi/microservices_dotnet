using Inventory.Application.Abstractions;
using Inventory.Domain.Stock;
using Store.SharedKernel;

namespace Inventory.Application.Stock;

public sealed record RenameProductCommand(Guid ProductId, string ProductName);

/// <summary>Keeps the locally replicated product name in sync with Catalog.</summary>
public sealed class RenameProductHandler(IStockItemRepository stockItems, IUnitOfWork unitOfWork)
    : ICommandHandler<RenameProductCommand>
{
    public async Task<Result> HandleAsync(RenameProductCommand command, CancellationToken cancellationToken = default)
    {
        var stockItem = await stockItems.GetAsync(command.ProductId, cancellationToken);
        if (stockItem is null)
        {
            return StockErrors.NotFound(command.ProductId);
        }

        stockItem.Rename(command.ProductName);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
