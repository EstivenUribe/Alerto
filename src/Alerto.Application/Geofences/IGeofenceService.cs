namespace Alerto.Application.Geofences;

public interface IGeofenceService
{
    Task<GeofenceListResponse> SearchAsync(GeofenceQueryRequest request, CancellationToken cancellationToken);
    Task<GeofenceResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<GeofenceResponse> CreateAsync(CreateGeofenceRequest request, CancellationToken cancellationToken);
    Task<GeofenceResponse> UpdateAsync(Guid id, UpdateGeofenceRequest request, CancellationToken cancellationToken);
    Task<GeofenceResponse> ActivateAsync(Guid id, ChangeGeofenceStatusRequest request, CancellationToken cancellationToken);
    Task<GeofenceResponse> DeactivateAsync(Guid id, ChangeGeofenceStatusRequest request, CancellationToken cancellationToken);
}
