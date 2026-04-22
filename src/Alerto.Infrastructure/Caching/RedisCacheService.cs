using System.Text.Json;
using Alerto.Application.Common.Interfaces;
using StackExchange.Redis;

namespace Alerto.Infrastructure.Caching;

public sealed class RedisCacheService : IAppCache
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public RedisCacheService(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
        where T : class
    {
        var database = _connectionMultiplexer.GetDatabase();
        var value = await database.StringGetAsync(key);
        if (value.IsNullOrEmpty)
        {
            return null;
        }

        return JsonSerializer.Deserialize<T>(value!, SerializerOptions);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken)
        where T : class
    {
        var database = _connectionMultiplexer.GetDatabase();
        var payload = JsonSerializer.Serialize(value, SerializerOptions);
        await database.StringSetAsync(key, payload, ttl);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        var database = _connectionMultiplexer.GetDatabase();
        await database.KeyDeleteAsync(key);
    }
}
