using Alerto.Application.Common.Interfaces;
using Alerto.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Alerto.Infrastructure.Persistence.Repositories;

public sealed class AlertCitizenConfirmationRepository : IAlertCitizenConfirmationRepository
{
    private readonly AlertoDbContext _dbContext;

    public AlertCitizenConfirmationRepository(AlertoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(AlertCitizenConfirmation confirmation, CancellationToken cancellationToken)
    {
        await _dbContext.AlertCitizenConfirmations.AddAsync(confirmation, cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid alertId, Guid userId, CancellationToken cancellationToken)
    {
        return _dbContext.AlertCitizenConfirmations
            .AnyAsync(x => x.AlertId == alertId && x.ConfirmedByUserId == userId, cancellationToken);
    }

    public async Task<AlertCitizenConfirmation[]> GetByAlertAsync(Guid alertId, CancellationToken cancellationToken)
    {
        return await _dbContext.AlertCitizenConfirmations
            .Where(x => x.AlertId == alertId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);
    }
}
