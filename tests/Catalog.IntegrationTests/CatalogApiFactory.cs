using Catalog.Infrastructure;
using Store.Testing;

namespace Catalog.IntegrationTests;

public sealed class CatalogApiFactory : ServiceApiFactory<Program>
{
    protected override string DatabaseConnectionName => DependencyInjection.DatabaseConnectionName;
}
