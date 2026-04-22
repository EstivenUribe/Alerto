using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Alerto.Api.HealthChecks;

public sealed class ApiHealthCheck : IHealthCheck
{
    private readonly IWebHostEnvironment _environment;

    public ApiHealthCheck(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(HealthCheckResult.Healthy(
            $"API operativa en ambiente '{_environment.EnvironmentName}'."));
    }
}
