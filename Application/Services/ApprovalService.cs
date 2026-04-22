using Alerto.Application.DTOs.Requests;
using Alerto.Application.DTOs.Responses;
using Alerto.Domain.Entities;
using Alerto.Domain.Exceptions;
using Alerto.Infrastructure.Persistence;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace Alerto.Application.Services;

/// <summary>
/// Implementación de servicios de aprobación de alertas.
/// </summary>
public class ApprovalService : IApprovalService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<ApprovalService> _logger;

    public ApprovalService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<ApprovalService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApproveAlertResponse> AprobarAlertaAsync(
        Guid id,
        int usuarioId,
        string? comentario,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var alerta = await _unitOfWork.Alerts.GetByIdAsync(id, cancellationToken);
            if (alerta == null)
            {
                _logger.LogWarning("Intento de aprobar alerta inexistente: {AlertId}", id);
                throw new AlertException($"Alerta {id} no encontrada");
            }

            // Validar que el operador exista
            var operador = await _unitOfWork.Usuarios.GetByIdAsync(usuarioId, cancellationToken);
            if (operador == null)
            {
                _logger.LogWarning("Intento de aprobar con operador inválido: {UsuarioId}", usuarioId);
                throw new AlertException($"Operador {usuarioId} no encontrado");
            }

            // Calcular tiempo de respuesta antes de llamar a Aprobar()
            var tiempoRespuestaSegundos = (int)(DateTime.UtcNow - alerta.TimestampGeneracion).TotalSeconds;
            
            // Llamar al método de dominio Aprobar() que valida todas las reglas de negocio
            alerta.Aprobar(usuarioId);
            
            _unitOfWork.Alerts.Update(alerta);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Alerta aprobada: {AlertId} por operador {OperadorId} en {Segundos} segundos",
                id, usuarioId, tiempoRespuestaSegundos);

            return new ApproveAlertResponse
            {
                Id = alerta.Id,
                Estado = "Aprobada",
                OperadorUsuarioId = usuarioId,
                OperadorNombre = operador.Nombre,
                TimestampAprobacion = DateTime.UtcNow,
                TiempoRespuestaSegundos = tiempoRespuestaSegundos,
                Difusion = new DifusionInfo
                {
                    Estado = "Pendiente de difusión",
                    Canales = new() { "Cell Broadcast" },
                    PoblacionObjetivo = alerta.PoblacionObjetivo
                }
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Error de negocio al aprobar alerta: {Mensaje}", ex.Message);
            throw new AlertException(ex.Message, ex);
        }
        catch (Exception ex) when (!(ex is AlertException))
        {
            _logger.LogError(ex, "Error al aprobar alerta: {AlertId}", id);
            throw new AlertException("Error al aprobar la alerta", ex);
        }
    }

    public async Task<AlertResponse> RechazarAlertaAsync(
        Guid id,
        int usuarioId,
        RejectAlertRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var alerta = await _unitOfWork.Alerts.GetByIdAsync(id, cancellationToken);
            if (alerta == null)
            {
                _logger.LogWarning("Intento de rechazar alerta inexistente: {AlertId}", id);
                throw new AlertException($"Alerta {id} no encontrada");
            }

            // Llamar al método de dominio Rechazar() que valida reglas de negocio
            alerta.Rechazar(usuarioId, request.Justificacion);
            
            _unitOfWork.Alerts.Update(alerta);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Alerta rechazada: {AlertId} por operador {OperadorId}",
                id, usuarioId);

            var response = _mapper.Map<AlertResponse>(alerta);
            return response;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Error de negocio al rechazar alerta: {Mensaje}", ex.Message);
            throw new AlertException(ex.Message, ex);
        }
        catch (Exception ex) when (!(ex is AlertException))
        {
            _logger.LogError(ex, "Error al rechazar alerta: {AlertId}", id);
            throw new AlertException("Error al rechazar la alerta", ex);
        }
    }

    public async Task<AlertResponse> CancelarAlertaAsync(
        Guid id,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var alerta = await _unitOfWork.Alerts.GetByIdAsync(id, cancellationToken);
            if (alerta == null)
            {
                _logger.LogWarning("Intento de cancelar alerta inexistente: {AlertId}", id);
                throw new AlertException($"Alerta {id} no encontrada");
            }

            // Llamar al método de dominio Cancelar() que valida reglas de negocio
            alerta.Cancelar(usuarioId);
            
            _unitOfWork.Alerts.Update(alerta);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Alerta cancelada: {AlertId} por operador {OperadorId}", id, usuarioId);

            var response = _mapper.Map<AlertResponse>(alerta);
            return response;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Error de negocio al cancelar alerta: {Mensaje}", ex.Message);
            throw new AlertException(ex.Message, ex);
        }
        catch (Exception ex) when (!(ex is AlertException))
        {
            _logger.LogError(ex, "Error al cancelar alerta: {AlertId}", id);
            throw new AlertException("Error al cancelar la alerta", ex);
        }
    }
}
