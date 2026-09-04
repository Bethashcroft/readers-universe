using System.Collections.Concurrent;

namespace ReadersRealm.Api.Services;

public class CoverBackfillLimiter
{
    private readonly ConcurrentDictionary<string, byte> _running = new();

    public async Task<T?> RunAsync<T>(string? userId, Func<Task<T>> work)
        where T : class
    {
        if (string.IsNullOrEmpty(userId) || !_running.TryAdd(userId, 0))
        {
            return null;
        }

        try
        {
            return await work();
        }
        finally
        {
            _running.TryRemove(userId, out _);
        }
    }
}
