using Alerto.Domain.Common;

namespace Alerto.Domain.Events;

public sealed record AlertRejectedDomainEvent(Guid AlertId, Guid RejectedByUserId, DateTime OccurredOnUtc)
    : DomainEvent(Guid.NewGuid(), OccurredOnUtc);
