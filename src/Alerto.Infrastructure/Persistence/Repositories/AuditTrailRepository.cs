using Alerto.Application.Common.Interfaces;
using Alerto.Domain.Entities;

namespace Alerto.Infrastructure.Persistence.Repositories;

public sealed class AuditTrailRepository : IAuditTrailRepository
{
    private readonly AlertoDbContext _dbContext;

    public AuditTrailRepository(AlertoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken)
    {
        await _dbContext.AuditLogs.AddAsync(auditLog, cancellationToken);
    }
}
