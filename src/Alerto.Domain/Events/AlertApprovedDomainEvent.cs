using Alerto.Domain.Common;

namespace Alerto.Domain.Events;

public sealed record AlertApprovedDomainEvent(Guid AlertId, Guid ApprovedByUserId, DateTime OccurredOnUtc)
    : DomainEvent(Guid.NewGuid(), OccurredOnUtc);
