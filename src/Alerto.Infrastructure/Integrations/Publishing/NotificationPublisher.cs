using System.Text.Json;
using Alerto.Application.Common.Interfaces;
using Alerto.Application.Common.Models;
using Alerto.Infrastructure.Integrations.Options;
using Alerto.Infrastructure.Persistence;
using Alerto.Infrastructure.Persistence.Outbox;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Alerto.Infrastructure.Integrations.Publishing;

public sealed class NotificationPublisher : INotificationPublisher
{
    private readonly AlertoDbContext _dbContext;
    private readonly NotificationPublisherOptions _options;
    private readonly ILogger<NotificationPublisher> _logger;

    public NotificationPublisher(
        AlertoDbContext dbContext,
        IOptions<NotificationPublisherOptions> options,
        ILogger<NotificationPublisher> logger)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _logger = logger;
    }

    public async Task PublishNotificationAsync(NotificationMessage message, CancellationToken cancellationToken)
    {
        if (_options.UseOutbox)
        {
            await EnqueueAsync("notification", message.CorrelationId, message, cancellationToken);
            return;
        }

        _logger.LogInformation(
            "Notification published directly. Topic={Topic} Subject={Subject} CorrelationId={CorrelationId} Simulated={SimulationMode}",
            message.Topic,
            message.Subject,
            message.CorrelationId,
            _options.SimulationMode);
    }

    public async Task PublishAuditEventAsync(AuditEventMessage message, CancellationToken cancellationToken)
    {
        if (_options.UseOutbox)
        {
            await EnqueueAsync("audit", message.CorrelationId, message, cancellationToken);
            return;
        }

        _logger.LogInformation(
            "Audit event published directly. Action={Action} Entity={EntityName} CorrelationId={CorrelationId} Simulated={SimulationMode}",
            message.Action,
            message.EntityName,
            message.CorrelationId,
            _options.SimulationMode);
    }

    private async Task EnqueueAsync<TPayload>(
        string type,
        string correlationId,
        TPayload payload,
        CancellationToken cancellationToken)
    {
        var outboxMessage = OutboxMessage.Create(
            type,
            JsonSerializer.Serialize(payload),
            correlationId,
            DateTime.UtcNow);

        await _dbContext.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
