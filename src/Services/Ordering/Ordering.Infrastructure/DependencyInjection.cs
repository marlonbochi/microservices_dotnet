using System.Data;
using System.Text.Json.Serialization;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ordering.Application.Abstractions;
using Ordering.Infrastructure.Catalog;
using Ordering.Infrastructure.DomainEvents;
using Ordering.Infrastructure.Messaging;
using Ordering.Infrastructure.Messaging.Sagas;
using Ordering.Infrastructure.Persistence;
using Ordering.Infrastructure.Realtime;
using Store.ServiceDefaults;
using Store.ServiceDefaults.Messaging;
using Store.SharedKernel;

namespace Ordering.Infrastructure;

public static class DependencyInjection
{
    public const string DatabaseConnectionName = "OrderingDb";
    public const string CatalogBaseUrlSetting = "Services:CatalogBaseUrl";

    public static IHostApplicationBuilder AddOrderingInfrastructure(this IHostApplicationBuilder builder)
    {
        AddPersistence(builder);
        AddCatalogClient(builder);

        builder.Services.AddSignalR()
            .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        builder.Services.AddScoped<IOrderNotifier, SignalROrderNotifier>();
        builder.Services.AddScoped<IIntegrationEventPublisher, OutboxEventPublisher>();

        builder.AddStoreMessaging(ConfigureMessaging);
        return builder;
    }

    /// <summary>Saga + bus outbox + consumer outbox. Public so tests reuse the exact same wiring.</summary>
    public static void ConfigureMessaging(IBusRegistrationConfigurator bus)
    {
        bus.AddSagaStateMachine<OrderStateMachine, OrderState>()
            .EntityFrameworkRepository(repository =>
            {
                repository.ConcurrencyMode = ConcurrencyMode.Optimistic;
                repository.ExistingDbContext<OrderingDbContext>();
                repository.UseSqlServer();
            });

        bus.AddEntityFrameworkOutbox<OrderingDbContext>(outbox =>
        {
            outbox.UseSqlServer();
            outbox.IsolationLevel = IsolationLevel.ReadCommitted;
            outbox.UseBusOutbox();
        });

        bus.AddConfigureEndpointsCallback((context, _, endpoint) =>
            endpoint.UseEntityFrameworkOutbox<OrderingDbContext>(context));
    }

    private static void AddPersistence(IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString(DatabaseConnectionName)
            ?? throw new InvalidOperationException($"Connection string '{DatabaseConnectionName}' is missing.");

        builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        builder.Services.AddDbContext<OrderingDbContext>(options => options.UseSqlServer(
            connectionString,
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", OrderingDbContext.Schema)));
        builder.Services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<OrderingDbContext>());
        builder.Services.AddScoped<IOrderRepository, OrderRepository>();

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<OrderingDbContext>("ordering-db", tags: [ServiceDefaultsExtensions.ReadyTag]);
    }

    private static void AddCatalogClient(IHostApplicationBuilder builder)
    {
        var catalogBaseUrl = builder.Configuration[CatalogBaseUrlSetting]
            ?? throw new InvalidOperationException($"Setting '{CatalogBaseUrlSetting}' is missing.");

        builder.Services.AddHttpClient<ICatalogClient, CatalogHttpClient>(client => client.BaseAddress = new Uri(catalogBaseUrl))
            .AddStandardResilienceHandler();
    }
}
