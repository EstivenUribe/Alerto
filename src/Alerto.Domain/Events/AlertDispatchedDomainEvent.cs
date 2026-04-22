using Alerto.Domain.Common;
using Alerto.Domain.Enums;

namespace Alerto.Domain.Events;

public sealed record AlertDispatchedDomainEvent(
    Guid AlertId,
    DispatchChannel Channel,
    Guid DispatchedByUserId,
    DateTime OccurredOnUtc)
    : DomainEvent(Guid.NewGuid(), OccurredOnUtc);
