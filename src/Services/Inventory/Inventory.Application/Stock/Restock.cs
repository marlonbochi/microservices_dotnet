using Inventory.Application.Abstractions;
using Inventory.Domain.Stock;
using Store.SharedKernel;

namespace Inventory.Application.Stock;

public sealed record RestockCommand(Guid ProductId, int Quantity);

public sealed class RestockHandler(IStockItemRepository stockItems, IUnitOfWork unitOfWork)
    : ICommandHandler<RestockCommand, StockItemResponse>
{
    public async Task<Result<StockItemResponse>> HandleAsync(RestockCommand command, CancellationToken cancellationToken = default)
    {
        var stockItem = await stockItems.GetAsync(command.ProductId, cancellationToken);
        if (stockItem is null)
        {
            return StockErrors.NotFound(command.ProductId);
        }

        var restocked = stockItem.Restock(command.Quantity);
        if (restocked.IsFailure)
        {
            return restocked.Error!;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return stockItem.ToResponse();
    }
}
