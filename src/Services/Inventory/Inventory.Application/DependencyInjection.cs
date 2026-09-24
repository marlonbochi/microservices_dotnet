using Inventory.Application.Reservations;
using Inventory.Application.Stock;
using Microsoft.Extensions.DependencyInjection;
using Store.SharedKernel;

namespace Inventory.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddInventoryApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<RegisterProductStockCommand>, RegisterProductStockHandler>();
        services.AddScoped<ICommandHandler<RenameProductCommand>, RenameProductHandler>();
        services.AddScoped<ICommandHandler<RestockCommand, StockItemResponse>, RestockHandler>();
        services.AddScoped<ICommandHandler<ReserveStockCommand>, ReserveStockHandler>();
        services.AddScoped<ICommandHandler<CommitStockCommand>, CommitStockHandler>();
        services.AddScoped<ICommandHandler<ReleaseStockCommand>, ReleaseStockHandler>();
        services.AddScoped<IQueryHandler<ListStockQuery, IReadOnlyList<StockItemResponse>>, ListStockHandler>();
        services.AddScoped<IQueryHandler<GetStockQuery, StockItemResponse?>, GetStockHandler>();
        return services;
    }
}
