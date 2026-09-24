namespace Store.Testing;

/// <summary>
/// Asynchronous systems are eventually consistent: poll until the condition holds or time runs out.
/// </summary>
public static class Eventually
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(200);

    public static async Task<T> Get<T>(Func<Task<T>> probe, Func<T, bool> condition, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? DefaultTimeout);
        while (true)
        {
            var value = await probe();
            if (condition(value) || DateTime.UtcNow > deadline)
            {
                return value;
            }

            await Task.Delay(PollInterval);
        }
    }
}
