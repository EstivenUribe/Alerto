using Alerto.Infrastructure.Integrations.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Alerto.Infrastructure.HealthChecks;

public sealed class NotificationPublisherHealthCheck : IHealthCheck
{
    private readonly NotificationPublisherOptions _options;
    private readonly OutboxOptions _outboxOptions;

    public NotificationPublisherHealthCheck(
        IOptions<NotificationPublisherOptions> options,
        IOptions<OutboxOptions> outboxOptions)
    {
        _options = options.Value;
        _outboxOptions = outboxOptions.Value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (_options.UseOutbox && !_outboxOptions.Enabled)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Publisher usa outbox pero el outbox esta deshabilitado."));
        }

        var description = _options.UseOutbox
            ? $"Publisher con outbox habilitado. Simulated={_options.SimulationMode}"
            : $"Publisher directo. Simulated={_options.SimulationMode}";

        return Task.FromResult(HealthCheckResult.Healthy(description));
    }
}
