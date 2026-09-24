using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Store.ServiceDefaults.Messaging;

/// <summary>Standard MassTransit + RabbitMQ setup shared by every service.</summary>
public static class MessagingExtensions
{
    public const string ConnectionStringName = "RabbitMq";

    private const int RetryLimit = 10;
    private static readonly TimeSpan MinRetryInterval = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan MaxRetryInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan RetryIntervalDelta = TimeSpan.FromMilliseconds(100);

    /// <param name="configure">Registers the service's consumers, sagas and outbox.</param>
    public static IHostApplicationBuilder AddStoreMessaging(
        this IHostApplicationBuilder builder,
        Action<IBusRegistrationConfigurator> configure)
    {
        var connectionString = builder.Configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is missing.");

        builder.Services.AddMassTransit(bus =>
        {
            bus.SetKebabCaseEndpointNameFormatter();

            // Registered as an endpoint callback (not on the transport) so it also applies when tests
            // swap RabbitMQ for the in-memory test harness. It runs before the outbox callback that
            // services register in 'configure', so every retry gets a fresh scope/DbContext.
            bus.AddConfigureEndpointsCallback((_, _, endpoint) => endpoint.UseMessageRetry(retry =>
                retry.Exponential(RetryLimit, MinRetryInterval, MaxRetryInterval, RetryIntervalDelta)));

            configure(bus);

            bus.UsingRabbitMq((context, rabbit) =>
            {
                rabbit.Host(new Uri(connectionString));
                rabbit.ConfigureEndpoints(context);
            });
        });

        return builder;
    }
}
