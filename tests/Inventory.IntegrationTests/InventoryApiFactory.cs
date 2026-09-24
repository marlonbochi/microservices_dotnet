using Inventory.Infrastructure;
using MassTransit;
using Store.Contracts.Inventory;
using Store.Testing;

namespace Inventory.IntegrationTests;

public sealed class InventoryApiFactory : ServiceApiFactory<Program>
{
    protected override string DatabaseConnectionName => DependencyInjection.DatabaseConnectionName;

    /// <summary>Stand-in for the order saga, which is the real consumer of these replies.</summary>
    protected override void ConfigureTestBus(IBusRegistrationConfigurator bus)
    {
        bus.AddConsumer<MessageCapture<StockReserved>>();
        bus.AddConsumer<MessageCapture<StockReservationFailed>>();
        bus.AddConsumer<MessageCapture<StockCommitted>>();
        bus.AddConsumer<MessageCapture<StockReleased>>();
    }
}
