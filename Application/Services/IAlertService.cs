using Alerto.Application.DTOs.Requests;
using Alerto.Application.DTOs.Responses;

namespace Alerto.Application.Services;

/// <summary>
/// Interfaz para operaciones sobre alertas.
/// </summary>
public interface IAlertService
{
    /// <summary>Crea una nueva alerta en estado Pendiente</summary>
    Task<AlertResponse> CrearAlertaAsync(CreateAlertRequest request, CancellationToken cancellationToken = default);

    /// <summary>Obtiene una alerta por su ID</summary>
    Task<AlertResponse?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Obtiene una alerta por su identificador CAP</summary>
    Task<AlertResponse?> ObtenerPorIdentificadorCapAsync(string identificadorCap, CancellationToken cancellationToken = default);

    /// <summary>Lista alertas con filtros opcionales y paginación</summary>
    Task<(IEnumerable<AlertResponse> Items, int TotalCount)> ListarAsync(
        string? status,
        int? geocercaId,
        DateTime? desde,
        DateTime? hasta,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
