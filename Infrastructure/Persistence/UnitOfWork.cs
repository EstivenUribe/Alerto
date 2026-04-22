using Alerto.Infrastructure.Repositories;
using Alerto.Domain.Entities;
using AutoMapper;

namespace Alerto.Infrastructure.Persistence;

/// <summary>
/// Implementación del patrón UnitOfWork que coordina repositorios.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly AlertoDbContext _dbContext;
    private readonly IMapper _mapper;
    private IAlertRepository? _alertRepository;
    private IRepository<Usuario>? _usuariosRepository;
    private IRepository<Geocerca>? _geocercasRepository;

    public UnitOfWork(AlertoDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public IAlertRepository Alerts
    {
        get
        {
            _alertRepository ??= new AlertRepository(_dbContext, _mapper);
            return _alertRepository;
        }
    }

    public IRepository<Usuario> Usuarios
    {
        get
        {
            _usuariosRepository ??= new Repository<Usuario>(_dbContext);
            return _usuariosRepository;
        }
    }

    public IRepository<Geocerca> Geocercas
    {
        get
        {
            _geocercasRepository ??= new Repository<Geocerca>(_dbContext);
            return _geocercasRepository;
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_dbContext != null)
        {
            await _dbContext.DisposeAsync();
        }
    }
}
