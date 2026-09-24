using Inventory.Application.Abstractions;
using Store.SharedKernel;

namespace Inventory.Application.Stock;

public sealed record ListStockQuery;

public sealed record GetStockQuery(Guid ProductId);

public sealed class ListStockHandler(IStockItemRepository stockItems)
    : IQueryHandler<ListStockQuery, IReadOnlyList<StockItemResponse>>
{
    public async Task<IReadOnlyList<StockItemResponse>> HandleAsync(ListStockQuery query, CancellationToken cancellationToken = default) =>
        [.. (await stockItems.ListAsync(cancellationToken)).Select(item => item.ToResponse())];
}

public sealed class GetStockHandler(IStockItemRepository stockItems)
    : IQueryHandler<GetStockQuery, StockItemResponse?>
{
    public async Task<StockItemResponse?> HandleAsync(GetStockQuery query, CancellationToken cancellationToken = default) =>
        (await stockItems.GetAsync(query.ProductId, cancellationToken))?.ToResponse();
}
