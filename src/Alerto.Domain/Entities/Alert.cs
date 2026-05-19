using Alerto.Domain.Common;
using Alerto.Domain.Enums;
using Alerto.Domain.Events;
using Alerto.Domain.Exceptions;
using Alerto.Domain.ValueObjects;

namespace Alerto.Domain.Entities;

public sealed class Alert : BaseEntity
{
    private readonly List<AlertDispatch> _dispatches = [];
    private readonly List<ApprovalRecord> _approvalRecords = [];

    private Alert()
    {
    }

    private Alert(
        string title,
        string description,
        Severity severity,
        string sourceSystem,
        string address,
        decimal latitude,
        decimal longitude,
        Guid geofenceId,
        Guid createdByUserId,
        int approvalTimeoutMinutes,
        DateTime utcNow)
    {
        Title = title;
        Description = description;
        Severity = severity;
        SourceSystem = Normalize(sourceSystem, nameof(sourceSystem), 80);
        Address = Normalize(address, nameof(address), 200);
        var coordinate = GeoCoordinate.Create(latitude, longitude);
        Latitude = coordinate.Latitude;
        Longitude = coordinate.Longitude;
        GeofenceId = geofenceId;
        CreatedByUserId = createdByUserId;
        Status = AlertStatus.Pending;
        ApprovalDeadlineUtc = utcNow.AddMinutes(approvalTimeoutMinutes);
        CreatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Severity Severity { get; private set; }
    public AlertStatus Status { get; private set; }
    public string SourceSystem { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public decimal Latitude { get; private set; }
    public decimal Longitude { get; private set; }
    public Guid GeofenceId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public Guid? RejectedByUserId { get; private set; }
    public Guid? CancelledByUserId { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }
    public DateTime? RejectedAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? CancellationReason { get; private set; }
    public bool IsDeleted { get; private set; }
    public Guid? DeletedByUserId { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }
    public string? DeletionReason { get; private set; }
    public DateTime ApprovalDeadlineUtc { get; private set; }
    public IReadOnlyCollection<AlertDispatch> Dispatches => _dispatches.AsReadOnly();
    public IReadOnlyCollection<ApprovalRecord> ApprovalRecords => _approvalRecords.AsReadOnly();
    public AlertContent Content => AlertContent.Create(Title, Description);
    public GeoCoordinate Location => GeoCoordinate.Create(Latitude, Longitude);

    public static Alert Create(
        string title,
        string description,
        Severity severity,
        string sourceSystem,
        string address,
        decimal latitude,
        decimal longitude,
        Guid geofenceId,
        Guid createdByUserId,
        DateTime utcNow,
        int approvalTimeoutMinutes = 3)
    {
        var content = AlertContent.Create(title, description);
        var alert = new Alert(
            content.Title,
            content.Description,
            severity,
            sourceSystem,
            address,
            latitude,
            longitude,
            geofenceId,
            createdByUserId,
            approvalTimeoutMinutes,
            utcNow);

        alert.RaiseDomainEvent(new AlertCreatedDomainEvent(alert.Id, createdByUserId, utcNow));
        return alert;
    }

    public void UpdateDraft(
        string title,
        string description,
        Severity severity,
        string sourceSystem,
        string address,
        decimal latitude,
        decimal longitude,
        Guid geofenceId,
        DateTime utcNow)
    {
        EnsurePending("update");
        var content = AlertContent.Create(title, description);
        var coordinate = GeoCoordinate.Create(latitude, longitude);

        Title = content.Title;
        Description = content.Description;
        Severity = severity;
        SourceSystem = Normalize(sourceSystem, nameof(sourceSystem), 80);
        Address = Normalize(address, nameof(address), 200);
        Latitude = coordinate.Latitude;
        Longitude = coordinate.Longitude;
        GeofenceId = geofenceId;
        Touch(utcNow);
        RaiseDomainEvent(new AlertUpdatedDomainEvent(Id, utcNow));
    }

    public void Approve(Guid approvedByUserId, DateTime utcNow)
    {
        EnsurePending("approve");

        if (utcNow > ApprovalDeadlineUtc)
        {
            throw new ApprovalWindowExpiredException();
        }

        var record = ApprovalRecord.Approve(Id, approvedByUserId, ApprovalDeadlineUtc, utcNow);
        _approvalRecords.Add(record);
        Status = AlertStatus.Approved;
        ApprovedByUserId = approvedByUserId;
        ApprovedAtUtc = utcNow;
        RejectedByUserId = null;
        RejectedAtUtc = null;
        RejectionReason = null;
        Touch(utcNow);
        RaiseDomainEvent(new AlertApprovedDomainEvent(Id, approvedByUserId, utcNow));
    }

    public void Reject(Guid rejectedByUserId, string reason, DateTime utcNow)
    {
        EnsurePending("reject");

        var record = ApprovalRecord.Reject(Id, rejectedByUserId, reason, ApprovalDeadlineUtc, utcNow);
        _approvalRecords.Add(record);
        Status = AlertStatus.Rejected;
        RejectedByUserId = rejectedByUserId;
        RejectedAtUtc = utcNow;
        RejectionReason = record.Reason;
        Touch(utcNow);
        RaiseDomainEvent(new AlertRejectedDomainEvent(Id, rejectedByUserId, utcNow));
    }

    public void Cancel(Guid cancelledByUserId, string reason, DateTime utcNow)
    {
        if (Status is AlertStatus.Rejected or AlertStatus.Cancelled)
        {
            throw new InvalidAlertStateTransitionException(Status, "cancel");
        }

        CancellationReason = Normalize(reason, nameof(reason), 500);
        Status = AlertStatus.Cancelled;
        CancelledByUserId = cancelledByUserId;
        CancelledAtUtc = utcNow;
        Touch(utcNow);
        RaiseDomainEvent(new AlertCancelledDomainEvent(Id, cancelledByUserId, utcNow));
    }

    public void DeleteAdministratively(Guid deletedByUserId, string reason, DateTime utcNow)
    {
        if (IsDeleted)
        {
            throw new DomainRuleException("La alerta ya fue eliminada administrativamente.");
        }

        IsDeleted = true;
        DeletedByUserId = deletedByUserId;
        DeletedAtUtc = utcNow;
        DeletionReason = Normalize(reason, nameof(reason), 500);
        Touch(utcNow);
    }

    public AlertDispatch Dispatch(
        DispatchChannel channel,
        string destination,
        string providerReference,
        Guid dispatchedByUserId,
        DateTime utcNow)
    {
        if (Status is not AlertStatus.Approved and not AlertStatus.Broadcasted)
        {
            throw new InvalidAlertStateTransitionException(Status, "dispatch");
        }

        var dispatch = AlertDispatch.Create(Id, channel, destination, providerReference, dispatchedByUserId, utcNow);
        _dispatches.Add(dispatch);
        Status = AlertStatus.Broadcasted;
        Touch(utcNow);
        RaiseDomainEvent(new AlertDispatchedDomainEvent(Id, channel, dispatchedByUserId, utcNow));
        return dispatch;
    }

    private void EnsurePending(string action)
    {
        if (Status != AlertStatus.Pending)
        {
            throw new InvalidAlertStateTransitionException(Status, action);
        }
    }

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
