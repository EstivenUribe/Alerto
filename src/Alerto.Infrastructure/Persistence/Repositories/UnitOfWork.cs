using Alerto.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Alerto.Infrastructure.Persistence.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AlertoDbContext _dbContext;

    public UnitOfWork(AlertoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new Application.Common.Exceptions.ConflictException(
                "Se detectó un conflicto de concurrencia optimista durante el guardado.")
            {
                Source = exception.Source
            };
        }
    }
}
