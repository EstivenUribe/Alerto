namespace Alerto.Domain.Entities;

[Obsolete("Use AuditLog instead.")]
public sealed class AuditTrail : AuditLog
{
    private AuditTrail()
    {
    }

    private AuditTrail(
        Guid actorId,
        string action,
        string entityName,
        Guid entityId,
        string detailsJson,
        string traceId,
        DateTime utcNow)
        : base(actorId, action, entityName, entityId, detailsJson, traceId, utcNow)
    {
    }

    public static new AuditTrail Create(
        Guid actorId,
        string action,
        string entityName,
        Guid entityId,
        string detailsJson,
        string traceId,
        DateTime utcNow)
        => new(actorId, action, entityName, entityId, detailsJson, traceId, utcNow);
}
