using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Ordering.Application.Abstractions;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Store.SharedKernel;

namespace Ordering.Infrastructure.Catalog;

/// <summary>
/// Typed HTTP client to the Catalog service. Retries, timeouts and the circuit breaker are added by
/// the standard resilience handler (see DependencyInjection); here we only translate failures.
/// </summary>
internal sealed partial class CatalogHttpClient(HttpClient httpClient, ILogger<CatalogHttpClient> logger) : ICatalogClient
{
    private const string BatchPath = "api/catalog/products/batch";

    public async Task<Result<IReadOnlyList<CatalogProduct>>> GetProductsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
    {
        var query = string.Join('&', ids.Select(id => $"ids={id}"));
        try
        {
            var products = await httpClient.GetFromJsonAsync<List<CatalogProduct>>($"{BatchPath}?{query}", cancellationToken);
            return products ?? [];
        }
        catch (Exception exception) when (IsTransient(exception, cancellationToken))
        {
            LogCatalogUnavailable(logger, exception);
            return Error.Unavailable("Catalog.Unavailable", "The catalog is temporarily unavailable. Please try again.");
        }
    }

    private static bool IsTransient(Exception exception, CancellationToken cancellationToken) =>
        exception is HttpRequestException or TimeoutRejectedException or BrokenCircuitException
        || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Catalog service unavailable")]
    private static partial void LogCatalogUnavailable(ILogger logger, Exception exception);
}
