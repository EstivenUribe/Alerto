using Alerto.Infrastructure.Integrations.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Alerto.Infrastructure.HealthChecks;

public sealed class SiataHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SiataOptions _options;

    public SiataHealthCheck(IHttpClientFactory httpClientFactory, IOptions<SiataOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (_options.SimulationMode)
        {
            return HealthCheckResult.Healthy("SIATA en modo simulado.");
        }

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(SiataHealthCheck));
            using var response = await client.GetAsync(_options.HealthPath, cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("SIATA disponible.")
                : HealthCheckResult.Unhealthy($"SIATA respondio con HTTP {(int)response.StatusCode}.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("SIATA no responde.", exception);
        }
    }
}
