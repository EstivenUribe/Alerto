using Alerto.Domain.Common;

namespace Alerto.Domain.Events;

public sealed record AlertCreatedDomainEvent(Guid AlertId, Guid CreatedByUserId, DateTime OccurredOnUtc)
    : DomainEvent(Guid.NewGuid(), OccurredOnUtc);
