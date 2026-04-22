using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Alerto.Infrastructure.HealthChecks;

public sealed class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public RedisHealthCheck(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var latency = await _connectionMultiplexer.GetDatabase().PingAsync();
            return HealthCheckResult.Healthy($"Redis disponible. Ping={latency.TotalMilliseconds}ms");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Redis no responde.", exception);
        }
    }
}
