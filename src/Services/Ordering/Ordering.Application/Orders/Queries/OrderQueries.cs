using Ordering.Application.Abstractions;
using Store.SharedKernel;

namespace Ordering.Application.Orders.Queries;

public sealed record GetOrderQuery(Guid Id);

public sealed record ListOrdersQuery(int Take);

public sealed class GetOrderHandler(IOrderRepository orders) : IQueryHandler<GetOrderQuery, OrderResponse?>
{
    public async Task<OrderResponse?> HandleAsync(GetOrderQuery query, CancellationToken cancellationToken = default) =>
        (await orders.GetByIdAsync(query.Id, cancellationToken))?.ToResponse();
}

public sealed class ListOrdersHandler(IOrderRepository orders) : IQueryHandler<ListOrdersQuery, IReadOnlyList<OrderResponse>>
{
    public async Task<IReadOnlyList<OrderResponse>> HandleAsync(ListOrdersQuery query, CancellationToken cancellationToken = default) =>
        [.. (await orders.ListRecentAsync(query.Take, cancellationToken)).Select(order => order.ToResponse())];
}
