using Alerto.Infrastructure.Persistence.Outbox;

namespace Alerto.Infrastructure.Integrations.Publishing;

internal interface IOutboxMessageDispatcher
{
    Task DispatchAsync(OutboxMessage message, CancellationToken cancellationToken);
}
