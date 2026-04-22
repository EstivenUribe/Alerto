using Alerto.Domain.Enums;
using Alerto.Application.Common.Models;

namespace Alerto.Application.Alerts;

public sealed record CreateAlertRequest(
    string Title,
    string Description,
    Severity Severity,
    string SourceSystem,
    string Address,
    decimal Latitude,
    decimal Longitude,
    Guid GeofenceId);

public sealed record UpdateAlertRequest(
    string Title,
    string Description,
    Severity Severity,
    string SourceSystem,
    string Address,
    decimal Latitude,
    decimal Longitude,
    Guid GeofenceId,
    int ExpectedVersion);

public sealed record AlertQueryRequest(
    AlertStatus? Status,
    Guid? GeofenceId,
    Severity? Severity,
    DateTime? CreatedFromUtc,
    DateTime? CreatedToUtc,
    int PageNumber = 1,
    int PageSize = 20);

public sealed record ApproveAlertRequest(int ExpectedVersion);

public sealed record RejectAlertRequest(int ExpectedVersion, string Reason);

public sealed record CancelAlertRequest(int ExpectedVersion, string Reason);

public sealed record DispatchAlertRequest(
    int ExpectedVersion,
    DispatchChannel Channel,
    string Destination,
    string ProviderReference);

public sealed record AlertDispatchResponse(
    Guid Id,
    string Channel,
    string Destination,
    string ProviderReference,
    Guid DispatchedByUserId,
    DateTime CreatedAtUtc);

public sealed record AlertResponse(
    Guid Id,
    string Title,
    string Description,
    string Severity,
    string Status,
    string SourceSystem,
    string Address,
    decimal Latitude,
    decimal Longitude,
    Guid GeofenceId,
    Guid CreatedByUserId,
    Guid? ApprovedByUserId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime ApprovalDeadlineUtc,
    int Version,
    IReadOnlyCollection<AlertDispatchResponse> Dispatches);

public sealed record AlertListResponse(
    IReadOnlyCollection<AlertResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);
