using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ordering.Application.Abstractions;
using Ordering.Infrastructure;
using Store.Contracts.Inventory;
using Store.Contracts.Payment;
using Store.SharedKernel;
using Store.Testing;

namespace Ordering.IntegrationTests;

/// <summary>Ordering with a real database; the Catalog HTTP dependency is replaced by a controllable fake.</summary>
public sealed class OrderingApiFactory : ServiceApiFactory<Program>
{
    public FakeCatalogClient Catalog { get; } = new();

    protected override string DatabaseConnectionName => DependencyInjection.DatabaseConnectionName;

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.RemoveAll<ICatalogClient>();
        services.AddSingleton<ICatalogClient>(Catalog);
    }

    /// <summary>Stand-ins for Inventory and Payment, which receive the saga's commands.</summary>
    protected override void ConfigureTestBus(IBusRegistrationConfigurator bus)
    {
        bus.AddConsumer<MessageCapture<ReserveStock>>();
        bus.AddConsumer<MessageCapture<ProcessPayment>>();
        bus.AddConsumer<MessageCapture<CommitStock>>();
        bus.AddConsumer<MessageCapture<ReleaseStock>>();
    }
}

public sealed class FakeCatalogClient : ICatalogClient
{
    private readonly Dictionary<Guid, CatalogProduct> _products = [];

    public bool Unavailable { get; set; }

    public CatalogProduct Add(string name, decimal price)
    {
        var product = new CatalogProduct(Guid.NewGuid(), name, price);
        _products[product.Id] = product;
        return product;
    }

    public Task<Result<IReadOnlyList<CatalogProduct>>> GetProductsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        Task.FromResult(Unavailable
            ? Result<IReadOnlyList<CatalogProduct>>.Failure(Error.Unavailable("Catalog.Unavailable", "down"))
            : Result.Success<IReadOnlyList<CatalogProduct>>([.. ids.Where(_products.ContainsKey).Select(id => _products[id])]));
}
