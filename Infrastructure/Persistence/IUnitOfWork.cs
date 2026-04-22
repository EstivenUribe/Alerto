using Alerto.Domain.Entities;
using Alerto.Infrastructure.Repositories;

namespace Alerto.Infrastructure.Persistence;

/// <summary>
/// Interfaz del patrón UnitOfWork para coordinar múltiples repositorios.
/// </summary>
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    /// <summary>Repositorio de alertas</summary>
    IAlertRepository Alerts { get; }

    /// <summary>Repositorio de usuarios</summary>
    IRepository<Usuario> Usuarios { get; }

    /// <summary>Repositorio de geocercas</summary>
    IRepository<Geocerca> Geocercas { get; }

    /// <summary>Guarda los cambios en la base de datos</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
