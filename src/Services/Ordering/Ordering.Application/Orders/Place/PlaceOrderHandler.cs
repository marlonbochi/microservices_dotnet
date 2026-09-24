using Ordering.Application.Abstractions;
using Ordering.Domain.Orders;
using Store.Contracts.Ordering;
using Store.SharedKernel;

namespace Ordering.Application.Orders.Place;

/// <summary>
/// Accepts an order: prices come from Catalog (sync query), then the order is stored together with
/// an <see cref="OrderSubmitted"/> event (outbox). Everything after that is asynchronous (saga).
/// </summary>
public sealed class PlaceOrderHandler(
    ICatalogClient catalog,
    IOrderRepository orders,
    IIntegrationEventPublisher publisher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ICommandHandler<PlaceOrderCommand, OrderResponse>
{
    public async Task<Result<OrderResponse>> HandleAsync(PlaceOrderCommand command, CancellationToken cancellationToken = default)
    {
        var productIds = command.Items.Select(item => item.ProductId).Distinct().ToArray();
        var lookup = await catalog.GetProductsAsync(productIds, cancellationToken);
        if (lookup.IsFailure)
        {
            return lookup.Error!;
        }

        var products = lookup.Value.ToDictionary(product => product.Id);
        var unknown = productIds.Where(id => !products.ContainsKey(id)).ToArray();
        if (unknown.Length > 0)
        {
            return Error.Validation("Order.UnknownProducts", $"Unknown product(s): {string.Join(", ", unknown)}.");
        }

        var items = command.Items.Select(line =>
            new OrderItem(line.ProductId, products[line.ProductId].Name, products[line.ProductId].Price, line.Quantity));
        var placed = Order.Place(command.CustomerName, command.CustomerEmail, items, command.SimulatePaymentFailure, timeProvider.GetUtcNow());
        if (placed.IsFailure)
        {
            return placed.Error!;
        }

        var order = placed.Value;
        orders.Add(order);
        await publisher.PublishAsync(ToIntegrationEvent(order), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return order.ToResponse();
    }

    private static OrderSubmitted ToIntegrationEvent(Order order) => new(
        order.Id,
        order.CustomerEmail,
        order.Total,
        order.SimulatePaymentFailure,
        [.. order.Items.Select(item => new OrderLine(item.ProductId, item.Quantity))],
        order.CreatedAt);
}
