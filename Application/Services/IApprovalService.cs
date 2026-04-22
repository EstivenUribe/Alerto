using Alerto.Application.DTOs.Requests;
using Alerto.Application.DTOs.Responses;

namespace Alerto.Application.Services;

/// <summary>
/// Interfaz para operaciones de aprobación y rechazo de alertas.
/// </summary>
public interface IApprovalService
{
    /// <summary>Aprueba una alerta en estado Pendiente</summary>
    Task<ApproveAlertResponse> AprobarAlertaAsync(
        Guid id,
        int usuarioId,
        string? comentario,
        CancellationToken cancellationToken = default);

    /// <summary>Rechaza una alerta en estado Pendiente</summary>
    Task<AlertResponse> RechazarAlertaAsync(
        Guid id,
        int usuarioId,
        RejectAlertRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Cancela una alerta en estado Aprobada o Difundida</summary>
    Task<AlertResponse> CancelarAlertaAsync(
        Guid id,
        int usuarioId,
        CancellationToken cancellationToken = default);
}
