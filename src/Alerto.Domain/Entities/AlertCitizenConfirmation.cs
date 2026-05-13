using Alerto.Domain.Common;

namespace Alerto.Domain.Entities;

public sealed class AlertCitizenConfirmation : BaseEntity
{
    private AlertCitizenConfirmation()
    {
    }

    private AlertCitizenConfirmation(Guid alertId, Guid confirmedByUserId, string notes, DateTime utcNow)
    {
        AlertId = alertId;
        ConfirmedByUserId = confirmedByUserId;
        Notes = notes;
        CreatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public Guid AlertId { get; private set; }
    public Guid ConfirmedByUserId { get; private set; }
    public string Notes { get; private set; } = string.Empty;

    public static AlertCitizenConfirmation Create(Guid alertId, Guid confirmedByUserId, string notes, DateTime utcNow) =>
        new(alertId, confirmedByUserId, notes.Trim(), utcNow);
}
