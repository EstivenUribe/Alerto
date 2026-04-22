using Alerto.Domain.Common;

namespace Alerto.Domain.Events;

public sealed record UserCreatedDomainEvent(Guid UserId, string Username, DateTime OccurredOnUtc)
    : DomainEvent(Guid.NewGuid(), OccurredOnUtc);
