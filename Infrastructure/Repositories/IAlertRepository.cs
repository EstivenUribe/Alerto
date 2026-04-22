using Alerto.Application.DTOs.Responses;
using Alerto.Domain.Entities;

namespace Alerto.Infrastructure.Repositories;

/// <summary>
/// Interfaz específica para operaciones sobre alertas con lógica de filtrado.
/// </summary>
public interface IAlertRepository : IRepository<Alert>
{
    /// <summary>Obtiene una alerta por su identificador CAP único</summary>
    Task<Alert?> GetByIdentificadorCapAsync(string identificadorCap, CancellationToken cancellationToken = default);

    /// <summary>Obtiene alertas por estado</summary>
    Task<IEnumerable<Alert>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);

    /// <summary>Obtiene alertas con filtros opcionales y paginación</summary>
    Task<(IEnumerable<AlertResponse> Items, int TotalCount)> GetWithFiltersAsync(
        string? status,
        int? geocercaId,
        DateTime? desde,
        DateTime? hasta,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
