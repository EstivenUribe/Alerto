using Alerto.Application.Alerts;
using Alerto.Application.Geofences;
using Alerto.Application.Users;
using Alerto.Domain.Entities;
using Alerto.Application.Common.Models;

namespace Alerto.Application.Common.Interfaces;

public interface IAlertRepository
{
    Task AddAsync(Alert alert, CancellationToken cancellationToken);
    Task<Alert?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResponse<Alert>> SearchAsync(AlertQueryRequest request, CancellationToken cancellationToken);
}

public interface IUserRepository
{
    Task AddAsync(User user, CancellationToken cancellationToken);
    Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
    Task<PagedResponse<User>> SearchAsync(UserQueryRequest request, CancellationToken cancellationToken);
}

public interface IGeofenceRepository
{
    Task AddAsync(Geofence geofence, CancellationToken cancellationToken);
    Task<Geofence?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Geofence?> GetByCodeAsync(string code, CancellationToken cancellationToken);
    Task<bool> ExistsActiveAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResponse<Geofence>> SearchAsync(GeofenceQueryRequest request, CancellationToken cancellationToken);
}

public interface IAuditTrailRepository
{
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
