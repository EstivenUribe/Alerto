using Microsoft.EntityFrameworkCore;
using Alerto.Infrastructure.Persistence;

namespace Alerto.Infrastructure.Repositories;

/// <summary>
/// Implementación genérica del patrón Repository para operaciones CRUD.
/// </summary>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly AlertoDbContext _dbContext;
    protected readonly DbSet<T> _dbSet;

    public Repository(AlertoDbContext dbContext)
    {
        _dbContext = dbContext;
        _dbSet = dbContext.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new[] { id }, cancellationToken);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    public virtual void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    public virtual void Delete(T entity)
    {
        _dbSet.Remove(entity);
    }

    public virtual void DeleteRange(IEnumerable<T> entities)
    {
        _dbSet.RemoveRange(entities);
    }

    public virtual async Task<bool> ExistsAsync(Func<T, bool> predicate, CancellationToken cancellationToken = default)
    {
        var items = await _dbSet.ToListAsync(cancellationToken);
        return items.Any(predicate);
    }
}
