using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Alerto.Infrastructure.HealthChecks;

public sealed class PostgresHealthCheck : IHealthCheck
{
    private readonly Persistence.AlertoDbContext _dbContext;

    public PostgresHealthCheck(Persistence.AlertoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy("PostgreSQL disponible.")
            : HealthCheckResult.Unhealthy("PostgreSQL no responde.");
    }
}
