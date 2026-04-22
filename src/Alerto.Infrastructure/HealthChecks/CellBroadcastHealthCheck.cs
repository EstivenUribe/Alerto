using Alerto.Infrastructure.Integrations.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Alerto.Infrastructure.HealthChecks;

public sealed class CellBroadcastHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CellBroadcastOptions _options;

    public CellBroadcastHealthCheck(IHttpClientFactory httpClientFactory, IOptions<CellBroadcastOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (_options.SimulationMode)
        {
            return HealthCheckResult.Healthy("Cell Broadcast en modo simulado.");
        }

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(CellBroadcastHealthCheck));
            using var response = await client.GetAsync(_options.HealthPath, cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("Cell Broadcast disponible.")
                : HealthCheckResult.Unhealthy($"Cell Broadcast respondio con HTTP {(int)response.StatusCode}.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Cell Broadcast no responde.", exception);
        }
    }
}
