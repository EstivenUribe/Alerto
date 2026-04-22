namespace Alerto.Domain.Common;

public abstract record DomainEvent(Guid EventId, DateTime OccurredOnUtc);
