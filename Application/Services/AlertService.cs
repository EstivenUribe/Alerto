using Alerto.Application.DTOs.Requests;
using Alerto.Application.DTOs.Responses;
using Alerto.Domain.Entities;
using Alerto.Domain.Exceptions;
using Alerto.Infrastructure.Persistence;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace Alerto.Application.Services;

/// <summary>
/// Implementación de servicios sobre alertas.
/// </summary>
public class AlertService : IAlertService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<AlertService> _logger;

    public AlertService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<AlertService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AlertResponse> CrearAlertaAsync(CreateAlertRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validar que la geocerca exista y esté activa
            var geocerca = await _unitOfWork.Geocercas.GetByIdAsync(request.GeocercaId, cancellationToken);
            if (geocerca == null || !geocerca.Activa)
            {
                _logger.LogWarning("Intento de crear alerta con geocerca inválida: {GeocercaId}", request.GeocercaId);
                throw new GeocercaInvalidException($"Geocerca {request.GeocercaId} no existe o no está activa");
            }

            // Validar que no exista otra alerta con el mismo IdentificadorCap
            var alertaExistente = await _unitOfWork.Alerts.GetByIdentificadorCapAsync(request.IdentificadorCap, cancellationToken);
            if (alertaExistente != null)
            {
                _logger.LogWarning("Intento de crear alerta duplicada: {IdentificadorCap}", request.IdentificadorCap);
                throw new AlertException($"Ya existe una alerta con el identificador {request.IdentificadorCap}");
            }

            // Crear la alerta usando el DTO
            var alerta = _mapper.Map<Alert>(request);
            
            await _unitOfWork.Alerts.AddAsync(alerta, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Alerta creada: {AlertId} - {IdentificadorCap}", alerta.Id, alerta.IdentificadorCap);

            var response = _mapper.Map<AlertResponse>(alerta);
            response.GeocercaNombre = geocerca.Nombre;
            return response;
        }
        catch (Exception ex) when (!(ex is AlertException || ex is GeocercaInvalidException))
        {
            _logger.LogError(ex, "Error al crear alerta: {IdentificadorCap}", request.IdentificadorCap);
            throw new AlertException("Error al crear la alerta", ex);
        }
    }

    public async Task<AlertResponse?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var alerta = await _unitOfWork.Alerts.GetByIdAsync(id, cancellationToken);
        if (alerta == null)
            return null;

        var response = _mapper.Map<AlertResponse>(alerta);
        if (alerta.Geocerca != null)
            response.GeocercaNombre = alerta.Geocerca.Nombre;

        return response;
    }

    public async Task<AlertResponse?> ObtenerPorIdentificadorCapAsync(string identificadorCap, CancellationToken cancellationToken = default)
    {
        var alerta = await _unitOfWork.Alerts.GetByIdentificadorCapAsync(identificadorCap, cancellationToken);
        if (alerta == null)
            return null;

        var response = _mapper.Map<AlertResponse>(alerta);
        if (alerta.Geocerca != null)
            response.GeocercaNombre = alerta.Geocerca.Nombre;

        return response;
    }

    public async Task<(IEnumerable<AlertResponse> Items, int TotalCount)> ListarAsync(
        string? status,
        int? geocercaId,
        DateTime? desde,
        DateTime? hasta,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Alerts.GetWithFiltersAsync(status, geocercaId, desde, hasta, page, pageSize, cancellationToken);
    }
}
