namespace Alerto.Infrastructure.Repositories;

/// <summary>
/// Interfaz genérica para operaciones CRUD sobre entidades.
/// </summary>
public interface IRepository<T> where T : class
{
    /// <summary>Obtiene una entidad por su ID</summary>
    Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default);

    /// <summary>Obtiene todas las entidades</summary>
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Agrega una nueva entidad</summary>
    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>Actualiza una entidad existente</summary>
    void Update(T entity);

    /// <summary>Elimina una entidad</summary>
    void Delete(T entity);

    /// <summary>Elimina múltiples entidades</summary>
    void DeleteRange(IEnumerable<T> entities);

    /// <summary>Verifica si existe una entidad que cumple la condición</summary>
    Task<bool> ExistsAsync(Func<T, bool> predicate, CancellationToken cancellationToken = default);
}
