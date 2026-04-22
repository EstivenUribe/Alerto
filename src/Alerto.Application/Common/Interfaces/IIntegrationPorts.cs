using Alerto.Application.Common.Models;

namespace Alerto.Application.Common.Interfaces;

public interface ISiataIntegrationClient
{
    Task<SiataHazardSnapshot> GetHazardSnapshotAsync(SiataHazardQuery query, CancellationToken cancellationToken);
}

public interface ICapGeneratorClient
{
    Task<CapAlertDocumentResponse> GenerateAsync(CapAlertDocumentRequest request, CancellationToken cancellationToken);
}

public interface ICellBroadcastDispatcher
{
    Task<CellBroadcastDispatchResponse> DispatchAsync(CellBroadcastDispatchRequest request, CancellationToken cancellationToken);
}

public interface INotificationPublisher
{
    Task PublishNotificationAsync(NotificationMessage message, CancellationToken cancellationToken);
    Task PublishAuditEventAsync(AuditEventMessage message, CancellationToken cancellationToken);
}

public interface IDispatchIdempotencyStore
{
    Task<bool> TryAcquireAsync(string key, TimeSpan ttl, CancellationToken cancellationToken);
    Task ReleaseAsync(string key, CancellationToken cancellationToken);
}
