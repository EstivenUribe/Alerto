namespace Alerto.Domain.Common;

public abstract class BaseEntity
{
    private readonly List<DomainEvent> _domainEvents = [];

    protected BaseEntity(Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; protected init; }
    public DateTime CreatedAtUtc { get; protected set; }
    public DateTime UpdatedAtUtc { get; protected set; }
    public int Version { get; private set; }
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Touch(DateTime utcNow)
    {
        UpdatedAtUtc = utcNow;
        Version++;
    }

    protected void RaiseDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
