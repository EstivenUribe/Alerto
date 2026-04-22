using Alerto.Domain.Common;
using Alerto.Domain.Exceptions;

namespace Alerto.Domain.Entities;

public class AuditLog : BaseEntity
{
    protected AuditLog()
    {
    }

    protected AuditLog(
        Guid actorId,
        string action,
        string entityName,
        Guid entityId,
        string detailsJson,
        string traceId,
        DateTime utcNow)
    {
        ActorId = actorId;
        Action = Normalize(action, nameof(action), 80);
        EntityName = Normalize(entityName, nameof(entityName), 80);
        EntityId = entityId;
        DetailsJson = string.IsNullOrWhiteSpace(detailsJson) ? "{}" : detailsJson;
        TraceId = Normalize(traceId, nameof(traceId), 100);
        CreatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public Guid ActorId { get; protected set; }
    public string Action { get; protected set; } = string.Empty;
    public string EntityName { get; protected set; } = string.Empty;
    public Guid EntityId { get; protected set; }
    public string DetailsJson { get; protected set; } = "{}";
    public string TraceId { get; protected set; } = string.Empty;

    public static AuditLog Create(
        Guid actorId,
        string action,
        string entityName,
        Guid entityId,
        string detailsJson,
        string traceId,
        DateTime utcNow)
        => new(actorId, action, entityName, entityId, detailsJson, traceId, utcNow);

    protected static string Normalize(string value, string fieldName, int maxLength)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new EntityValidationException($"El campo '{fieldName}' es obligatorio.");
        }

        if (normalized.Length > maxLength)
        {
            throw new EntityValidationException($"El campo '{fieldName}' no puede superar {maxLength} caracteres.");
        }

        return normalized;
    }
}
