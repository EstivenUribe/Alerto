using Microsoft.EntityFrameworkCore;
using Alerto.Infrastructure.Persistence;
using Alerto.Application.DTOs.Responses;
using Alerto.Domain.Entities;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using System.Linq;

namespace Alerto.Infrastructure.Repositories;

/// <summary>
/// Implementación específica para operaciones sobre alertas.
/// </summary>
public class AlertRepository : Repository<Alert>, IAlertRepository
{
    private readonly IMapper _mapper;

    public AlertRepository(AlertoDbContext dbContext, IMapper mapper) : base(dbContext)
    {
        _mapper = mapper;
    }

    public async Task<Alert?> GetByIdentificadorCapAsync(string identificadorCap, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(a => a.IdentificadorCap == identificadorCap, cancellationToken);
    }

    public async Task<IEnumerable<Alert>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => a.Estado.ToString() == status)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<AlertResponse> Items, int TotalCount)> GetWithFiltersAsync(
        string? status,
        int? geocercaId,
        DateTime? desde,
        DateTime? hasta,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        // Filtro por estado
        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(a => a.Estado.ToString() == status);
        }

        // Filtro por geocerca
        if (geocercaId.HasValue)
        {
            query = query.Where(a => a.GeocercaId == geocercaId.Value);
        }

        // Filtro por rango de fechas
        if (desde.HasValue)
        {
            query = query.Where(a => a.TimestampGeneracion >= desde.Value);
        }

        if (hasta.HasValue)
        {
            query = query.Where(a => a.TimestampGeneracion <= hasta.Value);
        }

        // Contar total antes de paginar
        var totalCount = await query.CountAsync(cancellationToken);

        // Paginación
        var items = await query
            .OrderByDescending(a => a.TimestampGeneracion)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(a => a.Geocerca)
            .Include(a => a.Operador)
            .ProjectTo<AlertResponse>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
