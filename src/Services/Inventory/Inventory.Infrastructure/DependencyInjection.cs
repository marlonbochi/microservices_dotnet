using Inventory.Application.Abstractions;
using Inventory.Infrastructure.Messaging;
using Inventory.Infrastructure.Messaging.Consumers;
using Inventory.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Store.ServiceDefaults;
using Store.ServiceDefaults.Messaging;
using Store.SharedKernel;

namespace Inventory.Infrastructure;

public static class DependencyInjection
{
    public const string DatabaseConnectionName = "InventoryDb";

    public static IHostApplicationBuilder AddInventoryInfrastructure(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString(DatabaseConnectionName)
            ?? throw new InvalidOperationException($"Connection string '{DatabaseConnectionName}' is missing.");

        builder.Services.AddDbContext<InventoryDbContext>(options => options.UseSqlServer(
            connectionString,
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", InventoryDbContext.Schema)));
        builder.Services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<InventoryDbContext>());
        builder.Services.AddScoped<IStockItemRepository, StockItemRepository>();
        builder.Services.AddScoped<IStockReservationRepository, StockReservationRepository>();
        builder.Services.AddScoped<IIntegrationEventPublisher, OutboxEventPublisher>();

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<InventoryDbContext>("inventory-db", tags: [ServiceDefaultsExtensions.ReadyTag]);

        builder.AddStoreMessaging(ConfigureMessaging);
        return builder;
    }

    /// <summary>Consumers + consumer outbox/inbox. Public so tests can reuse the exact same wiring.</summary>
    public static void ConfigureMessaging(IBusRegistrationConfigurator bus)
    {
        bus.AddConsumer<ProductCreatedConsumer>();
        bus.AddConsumer<ProductUpdatedConsumer>();
        bus.AddConsumer<ReserveStockConsumer>();
        bus.AddConsumer<CommitStockConsumer>();
        bus.AddConsumer<ReleaseStockConsumer>();

        bus.AddEntityFrameworkOutbox<InventoryDbContext>(outbox => outbox.UseSqlServer());

        // Inbox (de-duplication by MessageId) + outbox for messages published while consuming.
        bus.AddConfigureEndpointsCallback((context, _, endpoint) =>
            endpoint.UseEntityFrameworkOutbox<InventoryDbContext>(context));
    }
}
