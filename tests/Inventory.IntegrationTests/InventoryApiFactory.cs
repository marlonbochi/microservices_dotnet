using Inventory.Infrastructure;
using Store.Testing;

namespace Inventory.IntegrationTests;

public sealed class InventoryApiFactory : ServiceApiFactory<Program>
{
    protected override string DatabaseConnectionName => DependencyInjection.DatabaseConnectionName;
}
