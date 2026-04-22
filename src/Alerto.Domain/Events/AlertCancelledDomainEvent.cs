using Alerto.Domain.Common;

namespace Alerto.Domain.Events;

public sealed record AlertCancelledDomainEvent(Guid AlertId, Guid CancelledByUserId, DateTime OccurredOnUtc)
    : DomainEvent(Guid.NewGuid(), OccurredOnUtc);
