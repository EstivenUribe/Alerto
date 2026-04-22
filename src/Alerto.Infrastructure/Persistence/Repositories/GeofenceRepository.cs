using Alerto.Application.Common.Interfaces;
using Alerto.Application.Common.Models;
using Alerto.Application.Geofences;
using Alerto.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Alerto.Infrastructure.Persistence.Repositories;

public sealed class GeofenceRepository : IGeofenceRepository
{
    private readonly AlertoDbContext _dbContext;

    public GeofenceRepository(AlertoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Geofence geofence, CancellationToken cancellationToken)
    {
        await _dbContext.Geofences.AddAsync(geofence, cancellationToken);
    }

    public Task<Geofence?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Geofences.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Geofence?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        var normalizedCode = code.Trim();
        return _dbContext.Geofences.SingleOrDefaultAsync(x => x.Code == normalizedCode, cancellationToken);
    }

    public Task<bool> ExistsActiveAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Geofences.AnyAsync(x => x.Id == id && x.IsActive, cancellationToken);
    }

    public async Task<PagedResponse<Geofence>> SearchAsync(GeofenceQueryRequest request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Geofences.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.Code.ToLower().Contains(search) ||
                x.Name.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(request.Neighborhood))
        {
            var neighborhood = request.Neighborhood.Trim().ToLower();
            query = query.Where(x => x.Neighborhood.ToLower().Contains(neighborhood));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == request.IsActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)request.PageSize);
        var items = await query
            .OrderBy(x => x.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<Geofence>(
            items,
            request.PageNumber,
            request.PageSize,
            totalCount,
            totalPages,
            request.PageNumber > 1,
            totalPages > 0 && request.PageNumber < totalPages);
    }
}
