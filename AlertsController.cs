using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Alerto.Application.DTOs.Requests;
using Alerto.Application.DTOs.Responses;
using Alerto.Application.Services;

namespace Alerto.Api.Controllers.V1;

/// <summary>
/// Controlador de alertas civiles georreferenciadas.
/// Base path: /api/v1/alerts
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public class AlertsController : ControllerBase
{
    private readonly IAlertService _alertService;
    private readonly IApprovalService _approvalService;
    private readonly ILogger<AlertsController> _logger;

    public AlertsController(
        IAlertService alertService,
        IApprovalService approvalService,
        ILogger<AlertsController> logger)
    {
        _alertService = alertService;
        _approvalService = approvalService;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────
    // ENDPOINT 1: POST /api/v1/alerts — Crear alerta
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Crea una nueva alerta de inundación en estado Pendiente.
    /// </summary>
    /// <remarks>
    /// Puede ser invocado manualmente por un operador o automáticamente
    /// por el Rules Engine (M2M). La alerta queda asociada a una geocerca
    /// activa y debe ser aprobada antes de la difusión por Cell Broadcast.
    /// 
    /// Ejemplo de request:
    ///     POST /api/v1/alerts
    ///     {
    ///         "identificadorCap": "urn:oid:2.49.0.0.170.0.2026.04.14.001",
    ///         "severidad": "Warning",
    ///         "evento": "Inundación río Medellín - sector Barbosa",
    ///         "geocercaId": 42,
    ///         "mensajeEs": "ALERTA: Nivel del río supera umbral...",
    ///         "poblacionObjetivo": 15000
    ///     }
    /// </remarks>
    /// <param name="request">Datos de la alerta a crear</param>
    /// <returns>Alerta creada con su UUID asignado</returns>
    /// <response code="201">Alerta creada exitosamente</response>
    /// <response code="400">Datos de entrada inválidos</response>
    /// <response code="401">Token JWT ausente o inválido</response>
    /// <response code="403">Rol insuficiente</response>
    [HttpPost]
    [Authorize(Roles = "Operador,Administrador")]
    [ProducesResponseType(typeof(AlertResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CrearAlerta(
        [FromBody] CreateAlertRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Creando alerta para geocerca {GeocercaId}, severidad {Severidad}",
            request.GeocercaId, request.Severidad);

        var result = await _alertService.CrearAlertaAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(ObtenerAlerta),
            new { id = result.Id },
            result);
    }

    // ─────────────────────────────────────────────────────────
    // ENDPOINT 2: POST /api/v1/alerts/{id}/approve — Aprobar
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Aprueba una alerta pendiente para difusión por Cell Broadcast.
    /// </summary>
    /// <remarks>
    /// Cambia el estado de Pendiente → Aprobada, genera el mensaje CAP v1.2
    /// y dispara el flujo de difusión. Incluye validaciones de negocio:
    /// - Timeout de 3 minutos desde la creación
    /// - Geocerca activa
    /// - Control de concurrencia optimista
    /// - Registro de auditoría completo
    /// 
    /// Ejemplo:
    ///     POST /api/v1/alerts/550e8400-e29b-41d4-a716-446655440000/approve
    ///     {
    ///         "comentario": "Confirmado con estación SIATA."
    ///     }
    /// </remarks>
    /// <param name="id">UUID de la alerta a aprobar</param>
    /// <param name="request">Comentario opcional del operador</param>
    /// <returns>Resultado de la aprobación con estado de difusión</returns>
    /// <response code="200">Alerta aprobada y difusión iniciada</response>
    /// <response code="404">Alerta no encontrada</response>
    /// <response code="409">Conflicto de estado</response>
    /// <response code="422">Regla de negocio violada</response>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Operador,Administrador")]
    [ProducesResponseType(typeof(ApproveAlertResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AprobarAlerta(
        [FromRoute] Guid id,
        [FromBody] ApproveAlertRequest? request,
        CancellationToken cancellationToken)
    {
        // Extraer usuario del token JWT
        var usuarioId = int.Parse(
            User.FindFirst("sub")?.Value
            ?? throw new UnauthorizedAccessException());

        _logger.LogInformation(
            "Operador {UsuarioId} aprobando alerta {AlertaId}",
            usuarioId, id);

        var result = await _approvalService.AprobarAlertaAsync(
            id,
            usuarioId,
            request?.Comentario,
            cancellationToken);

        return Ok(result);
    }

    // ─────────────────────────────────────────────────────────
    // Endpoints auxiliares (referenciados por CreatedAtAction)
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Obtiene el detalle de una alerta específica.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AlertResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerAlerta(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _alertService.ObtenerPorIdAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Lista alertas con filtros opcionales.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AlertResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarAlertas(
        [FromQuery] string? status,
        [FromQuery] int? geocercaId,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _alertService.ListarAsync(
            status, geocercaId, desde, hasta, page, pageSize, cancellationToken);
        return Ok(result);
    }
}
