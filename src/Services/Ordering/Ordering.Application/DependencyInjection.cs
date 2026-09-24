using Microsoft.Extensions.DependencyInjection;
using Ordering.Application.Orders;
using Ordering.Application.Orders.Notifications;
using Ordering.Application.Orders.Place;
using Ordering.Application.Orders.Queries;
using Ordering.Domain.Orders;
using Store.SharedKernel;

namespace Ordering.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddOrderingApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<PlaceOrderCommand, OrderResponse>, PlaceOrderHandler>();
        services.AddScoped<IQueryHandler<GetOrderQuery, OrderResponse?>, GetOrderHandler>();
        services.AddScoped<IQueryHandler<ListOrdersQuery, IReadOnlyList<OrderResponse>>, ListOrdersHandler>();
        services.AddScoped<IDomainEventHandler<OrderStatusChangedDomainEvent>, NotifyOrderStatusChanged>();
        return services;
    }
}
