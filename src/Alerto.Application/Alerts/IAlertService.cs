using Alerto.Application.Common.Models;

namespace Alerto.Application.Alerts;

public interface IAlertService
{
    Task<AlertResponse> CreateAsync(CreateAlertRequest request, CancellationToken cancellationToken);
    Task<AlertResponse> UpdateAsync(Guid id, UpdateAlertRequest request, CancellationToken cancellationToken);
    Task<AlertResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<AlertListResponse> SearchAsync(AlertQueryRequest request, CancellationToken cancellationToken);
    Task<AlertResponse> ApproveAsync(Guid id, ApproveAlertRequest request, CancellationToken cancellationToken);
    Task<AlertResponse> RejectAsync(Guid id, RejectAlertRequest request, CancellationToken cancellationToken);
    Task<AlertResponse> CancelAsync(Guid id, CancelAlertRequest request, CancellationToken cancellationToken);
    Task<AlertResponse> DispatchAsync(Guid id, DispatchAlertRequest request, CancellationToken cancellationToken);
}
