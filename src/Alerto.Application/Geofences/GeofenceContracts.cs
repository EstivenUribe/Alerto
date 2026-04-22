using Alerto.Application.Common.Models;

namespace Alerto.Application.Geofences;

public sealed record CreateGeofenceRequest(string Code, string Name, string PolygonWkt, string Neighborhood);

public sealed record UpdateGeofenceRequest(string Name, string PolygonWkt, string Neighborhood, int ExpectedVersion);

public sealed record GeofenceQueryRequest(
    string? Search,
    string? Neighborhood,
    bool? IsActive,
    int PageNumber = 1,
    int PageSize = 20);

public sealed record ChangeGeofenceStatusRequest(int ExpectedVersion, string? Reason);

public sealed record GeofenceResponse(
    Guid Id,
    string Code,
    string Name,
    string PolygonWkt,
    string Neighborhood,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    int Version);

public sealed record GeofenceListResponse(
    IReadOnlyCollection<GeofenceResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);
