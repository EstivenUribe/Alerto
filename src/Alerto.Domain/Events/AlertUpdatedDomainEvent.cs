using Alerto.Domain.Common;

namespace Alerto.Domain.Events;

public sealed record AlertUpdatedDomainEvent(Guid AlertId, DateTime OccurredOnUtc)
    : DomainEvent(Guid.NewGuid(), OccurredOnUtc);
