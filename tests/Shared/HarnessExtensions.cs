using MassTransit.Testing;

namespace Store.Testing;

public static class HarnessExtensions
{
    /// <summary>
    /// Waits until a message of type <typeparamref name="T"/> matching the predicate was published
    /// directly or consumed by a <see cref="MessageCapture{T}"/>. Messages delivered by the outbox are
    /// re-sent as raw bytes, so only the consumed side can see them typed.
    /// Polling (instead of <c>Published.Any</c>) avoids the harness timeouts, which start counting
    /// when the shared factory boots.
    /// </summary>
    public static async Task<bool> WaitForPublishedAsync<T>(this ITestHarness harness, Func<T, bool> predicate, TimeSpan? timeout = null)
        where T : class =>
        await Eventually.Get(
            () => Task.FromResult(CountPublished(harness, predicate) > 0),
            found => found,
            timeout);

    public static int CountPublished<T>(this ITestHarness harness, Func<T, bool> predicate)
        where T : class
    {
        var published = harness.Published.Select<T>(CancellationToken.None).Select(message => message.Context.Message);
        var consumed = harness.Consumed.Select<T>(CancellationToken.None).Select(message => message.Context.Message);

        // Records compare by value, so a message seen in both lists is counted once.
        return published.Concat(consumed).Where(predicate).Distinct().Count();
    }
}
