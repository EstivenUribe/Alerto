namespace Alerto.Infrastructure.Persistence.Outbox;

public sealed class OutboxMessage
{
    private OutboxMessage()
    {
    }

    private OutboxMessage(string type, string payloadJson, string correlationId, DateTime occurredAtUtc)
    {
        Id = Guid.NewGuid();
        Type = type;
        PayloadJson = payloadJson;
        CorrelationId = correlationId;
        OccurredAtUtc = occurredAtUtc;
        CreatedAtUtc = occurredAtUtc;
    }

    public Guid Id { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = string.Empty;
    public string CorrelationId { get; private set; } = string.Empty;
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }

    public static OutboxMessage Create(string type, string payloadJson, string correlationId, DateTime occurredAtUtc)
        => new(type.Trim(), payloadJson.Trim(), correlationId.Trim(), occurredAtUtc);

    public void MarkProcessed(DateTime utcNow)
    {
        Attempts++;
        ProcessedAtUtc = utcNow;
        LastError = null;
    }

    public void MarkFailed(string error, DateTime utcNow)
    {
        Attempts++;
        LastError = error;
    }
}
