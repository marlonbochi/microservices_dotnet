using System.Data.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Store.ServiceDefaults;

/// <summary>
/// Applies pending EF Core migrations at startup (demo convenience). SQL Server can take a while
/// to accept connections inside Docker, so we retry for a bounded time.
/// </summary>
public static partial class DatabaseMigrationExtensions
{
    public const string ApplyMigrationsSetting = "Database:ApplyMigrationsOnStartup";

    private const int MaxAttempts = 20;
    private static readonly TimeSpan DelayBetweenAttempts = TimeSpan.FromSeconds(3);

    public static async Task ApplyMigrationsAsync<TContext>(this WebApplication app)
        where TContext : DbContext
    {
        if (!app.Configuration.GetValue(ApplyMigrationsSetting, defaultValue: false))
        {
            return;
        }

        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DatabaseMigrationExtensions));
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var scope = app.Services.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<TContext>().Database.MigrateAsync();
                LogMigrated(logger, typeof(TContext).Name);
                return;
            }
            // Only connection/database errors are worth retrying (SQL Server still booting); anything else
            // (e.g. a configuration bug) must fail fast instead of hanging startup for a minute.
            catch (DbException exception) when (attempt < MaxAttempts)
            {
                LogRetrying(logger, typeof(TContext).Name, attempt, exception.Message);
                await Task.Delay(DelayBetweenAttempts);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Database for {Context} is up to date")]
    private static partial void LogMigrated(ILogger logger, string context);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Migrating {Context} failed (attempt {Attempt}): {Reason}. Retrying...")]
    private static partial void LogRetrying(ILogger logger, string context, int attempt, string reason);
}
