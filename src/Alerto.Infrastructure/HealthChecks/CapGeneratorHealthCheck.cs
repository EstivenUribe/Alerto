using Alerto.Infrastructure.Integrations.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Alerto.Infrastructure.HealthChecks;

public sealed class CapGeneratorHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CapGeneratorOptions _options;

    public CapGeneratorHealthCheck(IHttpClientFactory httpClientFactory, IOptions<CapGeneratorOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (_options.SimulationMode)
        {
            return HealthCheckResult.Healthy("CAP Generator en modo simulado.");
        }

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(CapGeneratorHealthCheck));
            using var response = await client.GetAsync(_options.HealthPath, cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("CAP Generator disponible.")
                : HealthCheckResult.Unhealthy($"CAP Generator respondio con HTTP {(int)response.StatusCode}.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("CAP Generator no responde.", exception);
        }
    }
}
