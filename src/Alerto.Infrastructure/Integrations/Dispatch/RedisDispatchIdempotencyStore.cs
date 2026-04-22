using Alerto.Application.Common.Interfaces;
using StackExchange.Redis;

namespace Alerto.Infrastructure.Integrations.Dispatch;

public sealed class RedisDispatchIdempotencyStore : IDispatchIdempotencyStore
{
    private const string Prefix = "dispatch:idempotency:";
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public RedisDispatchIdempotencyStore(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    public async Task<bool> TryAcquireAsync(string key, TimeSpan ttl, CancellationToken cancellationToken)
    {
        var database = _connectionMultiplexer.GetDatabase();
        return await database.StringSetAsync(BuildKey(key), "1", ttl, when: When.NotExists);
    }

    public async Task ReleaseAsync(string key, CancellationToken cancellationToken)
    {
        var database = _connectionMultiplexer.GetDatabase();
        await database.KeyDeleteAsync(BuildKey(key));
    }

    private static string BuildKey(string key) => $"{Prefix}{key}";
}
