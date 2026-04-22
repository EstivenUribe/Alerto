using Alerto.Infrastructure.Integrations.Options;
using Alerto.Infrastructure.Persistence.Outbox;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Alerto.Infrastructure.Integrations.Publishing;

internal sealed class LoggingOutboxMessageDispatcher : IOutboxMessageDispatcher
{
    private readonly NotificationPublisherOptions _options;
    private readonly ILogger<LoggingOutboxMessageDispatcher> _logger;

    public LoggingOutboxMessageDispatcher(
        IOptions<NotificationPublisherOptions> options,
        ILogger<LoggingOutboxMessageDispatcher> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task DispatchAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Outbox message dispatched. Type={Type} CorrelationId={CorrelationId} Simulated={SimulationMode}",
            message.Type,
            message.CorrelationId,
            _options.SimulationMode);

        return Task.CompletedTask;
    }
}
