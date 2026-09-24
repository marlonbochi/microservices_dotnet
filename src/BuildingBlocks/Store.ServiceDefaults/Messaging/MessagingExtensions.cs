using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Store.ServiceDefaults.Messaging;

/// <summary>Standard MassTransit + RabbitMQ setup shared by every service.</summary>
public static class MessagingExtensions
{
    public const string ConnectionStringName = "RabbitMq";

    private const int RetryLimit = 5;
    private static readonly TimeSpan MinRetryInterval = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan MaxRetryInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan RetryIntervalDelta = TimeSpan.FromMilliseconds(200);

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
            configure(bus);

            bus.UsingRabbitMq((context, rabbit) =>
            {
                rabbit.Host(new Uri(connectionString));
                rabbit.UseMessageRetry(retry => retry.Exponential(
                    RetryLimit, MinRetryInterval, MaxRetryInterval, RetryIntervalDelta));
                rabbit.ConfigureEndpoints(context);
            });
        });

        return builder;
    }
}
