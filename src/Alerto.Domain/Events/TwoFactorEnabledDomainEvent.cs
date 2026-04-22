using Alerto.Domain.Common;

namespace Alerto.Domain.Events;

public sealed record TwoFactorEnabledDomainEvent(Guid UserId, DateTime OccurredOnUtc)
    : DomainEvent(Guid.NewGuid(), OccurredOnUtc);
