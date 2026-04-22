namespace Alerto.Application.Common.Models;

public sealed record SiataHazardQuery(
    decimal Latitude,
    decimal Longitude,
    Guid? GeofenceId,
    string? Neighborhood);

public sealed record SiataHazardSnapshot(
    string Source,
    string Summary,
    string RiskLevel,
    DateTime ObservedAtUtc,
    string RawPayloadJson,
    bool IsSimulated);

public sealed record CapAlertDocumentRequest(
    Guid AlertId,
    string Identifier,
    string Sender,
    string Title,
    string Description,
    string Severity,
    string AreaDescription,
    DateTime EffectiveAtUtc,
    DateTime ExpiresAtUtc);

public sealed record CapAlertDocumentResponse(
    string DocumentId,
    string ContentType,
    string Content,
    DateTime GeneratedAtUtc,
    bool IsSimulated);

public sealed record CellBroadcastDispatchRequest(
    Guid AlertId,
    string Message,
    string AreaCode,
    string Severity,
    string ProviderReference,
    string? Language = "es-CO");

public sealed record CellBroadcastDispatchResponse(
    string ProviderMessageId,
    string Status,
    bool DuplicateSuppressed,
    bool IsSimulated,
    DateTime AcceptedAtUtc);

public sealed record NotificationMessage(
    Guid MessageId,
    string Topic,
    string Subject,
    string Body,
    string CorrelationId,
    IDictionary<string, string>? Metadata);

public sealed record AuditEventMessage(
    Guid MessageId,
    string Action,
    string EntityName,
    Guid EntityId,
    string CorrelationId,
    string PayloadJson,
    DateTime OccurredAtUtc);
