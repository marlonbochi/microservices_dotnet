using MassTransit;
using MassTransit.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace Store.Testing;

/// <summary>
/// Boots a real service in memory (WebApplicationFactory) against a real SQL Server started by
/// Testcontainers. RabbitMQ is swapped for MassTransit's in-memory test harness, keeping the
/// service's consumers, sagas, retry and outbox configuration.
/// </summary>
public abstract class ServiceApiFactory<TProgram> : WebApplicationFactory<TProgram>, IAsyncLifetime
    where TProgram : class
{
    private const string SqlServerImage = "mcr.microsoft.com/mssql/server:2022-latest";
    private readonly MsSqlContainer _sqlServer = new MsSqlBuilder(SqlServerImage).Build();

    protected abstract string DatabaseConnectionName { get; }

    public string ConnectionString { get; private set; } = string.Empty;

    public ITestHarness Harness => Services.GetTestHarness();

    public async ValueTask InitializeAsync()
    {
        await _sqlServer.StartAsync();
        ConnectionString = new SqlConnectionStringBuilder(_sqlServer.GetConnectionString())
        {
            InitialCatalog = DatabaseConnectionName,
        }.ConnectionString;
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _sqlServer.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting($"ConnectionStrings:{DatabaseConnectionName}", ConnectionString);
        builder.UseSetting("ConnectionStrings:RabbitMq", "amqp://guest:guest@localhost:5672");
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
        builder.ConfigureTestServices(services =>
        {
            services.AddMassTransitTestHarness(ConfigureTestBus);
            ConfigureTestServices(services);
        });
    }

    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
    }

    /// <summary>Registers extra consumers that stand in for the other services (see <see cref="MessageCapture{T}"/>).</summary>
    protected virtual void ConfigureTestBus(IBusRegistrationConfigurator bus)
    {
    }
}
