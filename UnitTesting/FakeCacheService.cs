using Infra.Services;

namespace UnitTesting;

public class FakeCacheService: ICacheService
{
    private readonly Dictionary<string, object?> _store = new();
    public List<string> RemovedKeys { get; } = new();
    
    public Task<T?> GetAsync<T>(string key)
    {
        return Task.FromResult(_store.TryGetValue(key, out var value) ? (T?)value : default);
    }

    public Task SetAsync<T>(string key, T? value, TimeSpan expirationTime)
    {
        _store[key] = value;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _store.Remove(key);
        RemovedKeys.Add(key);
        return Task.CompletedTask;
    }
}