using System.Data;
using Catalog.Application.Abstractions;
using Catalog.Infrastructure.Messaging;
using Catalog.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Store.ServiceDefaults;
using Store.ServiceDefaults.Messaging;
using Store.SharedKernel;

namespace Catalog.Infrastructure;

public static class DependencyInjection
{
    public const string DatabaseConnectionName = "CatalogDb";

    public static IHostApplicationBuilder AddCatalogInfrastructure(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString(DatabaseConnectionName)
            ?? throw new InvalidOperationException($"Connection string '{DatabaseConnectionName}' is missing.");

        builder.Services.AddDbContext<CatalogDbContext>(options => options.UseSqlServer(
            connectionString,
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", CatalogDbContext.Schema)));
        builder.Services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<CatalogDbContext>());
        builder.Services.AddScoped<IProductRepository, ProductRepository>();
        builder.Services.AddScoped<IIntegrationEventPublisher, OutboxEventPublisher>();

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<CatalogDbContext>("catalog-db", tags: [ServiceDefaultsExtensions.ReadyTag]);

        builder.AddStoreMessaging(bus => bus.AddEntityFrameworkOutbox<CatalogDbContext>(outbox =>
        {
            outbox.UseSqlServer();
            outbox.IsolationLevel = IsolationLevel.ReadCommitted;
            outbox.UseBusOutbox();
        }));

        return builder;
    }
}
