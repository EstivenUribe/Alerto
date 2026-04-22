using Alerto.Domain.Common;
using Alerto.Domain.Enums;
using Alerto.Domain.Exceptions;

namespace Alerto.Domain.Entities;

public sealed class AlertDispatch : BaseEntity
{
    private AlertDispatch()
    {
    }

    private AlertDispatch(
        Guid alertId,
        DispatchChannel channel,
        string destination,
        string providerReference,
        Guid dispatchedByUserId,
        DateTime utcNow)
    {
        AlertId = alertId;
        Channel = channel;
        Destination = destination;
        ProviderReference = providerReference;
        DispatchedByUserId = dispatchedByUserId;
        CreatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public Guid AlertId { get; private set; }
    public DispatchChannel Channel { get; private set; }
    public string Destination { get; private set; } = string.Empty;
    public string ProviderReference { get; private set; } = string.Empty;
    public Guid DispatchedByUserId { get; private set; }

    public static AlertDispatch Create(
        Guid alertId,
        DispatchChannel channel,
        string destination,
        string providerReference,
        Guid dispatchedByUserId,
        DateTime utcNow)
        => new(
            alertId,
            channel,
            Normalize(destination, nameof(destination), 160),
            Normalize(providerReference, nameof(providerReference), 160),
            dispatchedByUserId,
            utcNow);

    private static string Normalize(string value, string fieldName, int maxLength)
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
