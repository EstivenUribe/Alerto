using Alerto.Domain.Common;
using Alerto.Domain.Enums;
using Alerto.Domain.Exceptions;

namespace Alerto.Domain.Entities;

public sealed class ApprovalRecord : BaseEntity
{
    private ApprovalRecord()
    {
    }

    private ApprovalRecord(
        Guid alertId,
        Guid reviewedByUserId,
        ApprovalDecision decision,
        string? reason,
        DateTime approvalDeadlineUtc,
        DateTime reviewedAtUtc)
    {
        AlertId = alertId;
        ReviewedByUserId = reviewedByUserId;
        Decision = decision;
        Reason = NormalizeReason(reason, decision);
        ApprovalDeadlineUtc = approvalDeadlineUtc;
        ReviewedAtUtc = reviewedAtUtc;
        CreatedAtUtc = reviewedAtUtc;
        UpdatedAtUtc = reviewedAtUtc;
    }

    public Guid AlertId { get; private set; }
    public Guid ReviewedByUserId { get; private set; }
    public ApprovalDecision Decision { get; private set; }
    public string? Reason { get; private set; }
    public DateTime ApprovalDeadlineUtc { get; private set; }
    public DateTime ReviewedAtUtc { get; private set; }

    public static ApprovalRecord Approve(Guid alertId, Guid reviewedByUserId, DateTime approvalDeadlineUtc, DateTime reviewedAtUtc)
        => new(alertId, reviewedByUserId, ApprovalDecision.Approved, null, approvalDeadlineUtc, reviewedAtUtc);

    public static ApprovalRecord Reject(
        Guid alertId,
        Guid reviewedByUserId,
        string reason,
        DateTime approvalDeadlineUtc,
        DateTime reviewedAtUtc)
        => new(alertId, reviewedByUserId, ApprovalDecision.Rejected, reason, approvalDeadlineUtc, reviewedAtUtc);

    private static string? NormalizeReason(string? reason, ApprovalDecision decision)
    {
        var normalized = reason?.Trim();

        if (decision == ApprovalDecision.Rejected && string.IsNullOrWhiteSpace(normalized))
        {
            throw new EntityValidationException("El rechazo de una alerta exige una justificacion.");
        }

        if (!string.IsNullOrWhiteSpace(normalized) && normalized.Length > 500)
        {
            throw new EntityValidationException("La justificacion de la decision no puede superar 500 caracteres.");
        }

        return normalized;
    }
}
