using Alerto.Application.Alerts;
using Alerto.Application.Common.Interfaces;
using Alerto.Application.Common.Models;
using Alerto.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Alerto.Infrastructure.Persistence.Repositories;

public sealed class AlertRepository : IAlertRepository
{
    private readonly AlertoDbContext _dbContext;

    public AlertRepository(AlertoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Alert alert, CancellationToken cancellationToken)
    {
        await _dbContext.Alerts.AddAsync(alert, cancellationToken);
    }

    public async Task<Alert?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Alerts
            .Include(x => x.Dispatches)
            .SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
    }

    public async Task<PagedResponse<Alert>> SearchAsync(AlertQueryRequest request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Alerts
            .Include(x => x.Dispatches)
            .Where(alert => !alert.IsDeleted)
            .AsNoTracking()
            .AsQueryable();

        if (request.Status.HasValue)
        {
            query = query.Where(alert => alert.Status == request.Status.Value);
        }

        if (request.GeofenceId.HasValue)
        {
            query = query.Where(alert => alert.GeofenceId == request.GeofenceId.Value);
        }

        if (request.Severity.HasValue)
        {
            query = query.Where(alert => alert.Severity == request.Severity.Value);
        }

        if (request.CreatedFromUtc.HasValue)
        {
            query = query.Where(alert => alert.CreatedAtUtc >= request.CreatedFromUtc.Value);
        }

        if (request.CreatedToUtc.HasValue)
        {
            query = query.Where(alert => alert.CreatedAtUtc <= request.CreatedToUtc.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(alert => alert.CreatedAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new PagedResponse<Alert>(
            items,
            request.PageNumber,
            request.PageSize,
            totalCount,
            totalPages,
            request.PageNumber > 1,
            request.PageNumber < totalPages);
    }
}
