using Alerto.Application.Geofences;
using Alerto.Api.Security;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alerto.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/geofences")]
[Authorize(Policy = AuthPolicies.GeofenceReaders)]
public sealed class GeofencesController : ControllerBase
{
    private readonly IGeofenceService _geofenceService;

    public GeofencesController(IGeofenceService geofenceService)
    {
        _geofenceService = geofenceService;
    }

    /// <summary>
    /// Lista geocercas con filtros de operacion para poblar el tablero y el panel administrativo.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(GeofenceListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GeofenceListResponse>> Search(
        [FromQuery] GeofenceQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _geofenceService.SearchAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Obtiene el detalle completo de una geocerca para administracion y trazabilidad operativa.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GeofenceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GeofenceResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await _geofenceService.GetByIdAsync(id, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Crea una nueva geocerca administrable por el panel para clasificar alertas por zona.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = AuthPolicies.GeofenceManagers)]
    [ProducesResponseType(typeof(GeofenceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GeofenceResponse>> Create(
        [FromBody] CreateGeofenceRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _geofenceService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id, version = "1" }, response);
    }

    /// <summary>
    /// Actualiza nombre operativo, poligono y barrio de referencia de una geocerca existente.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthPolicies.GeofenceManagers)]
    [ProducesResponseType(typeof(GeofenceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GeofenceResponse>> Update(
        Guid id,
        [FromBody] UpdateGeofenceRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _geofenceService.UpdateAsync(id, request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Reactiva una geocerca para que vuelva a estar disponible en la operacion del tablero.
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = AuthPolicies.GeofenceManagers)]
    [ProducesResponseType(typeof(GeofenceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GeofenceResponse>> Activate(
        Guid id,
        [FromBody] ChangeGeofenceStatusRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _geofenceService.ActivateAsync(id, request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Inactiva una geocerca sin borrarla para preservar trazabilidad y evitar nuevas asignaciones.
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = AuthPolicies.GeofenceManagers)]
    [ProducesResponseType(typeof(GeofenceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GeofenceResponse>> Deactivate(
        Guid id,
        [FromBody] ChangeGeofenceStatusRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _geofenceService.DeactivateAsync(id, request, cancellationToken);
        return Ok(response);
    }
}
