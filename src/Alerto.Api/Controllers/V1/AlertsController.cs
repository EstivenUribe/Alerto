using Alerto.Application.Alerts;
using Alerto.Api.Security;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alerto.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/alerts")]
[Authorize(Policy = AuthPolicies.AlertReaders)]
public sealed class AlertsController : ControllerBase
{
    private readonly IAlertService _alertService;

    public AlertsController(IAlertService alertService)
    {
        _alertService = alertService;
    }

    /// <summary>
    /// Lista alertas con filtros por estado, geocerca, severidad y rango de fecha, con paginación.
    /// </summary>
    /// <param name="request">Filtros opcionales de búsqueda y parámetros de paginación.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Resultado paginado de alertas.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(AlertListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AlertListResponse>> Search(
        [FromQuery] AlertQueryRequest request,
        CancellationToken cancellationToken)
    {
        var alerts = await _alertService.SearchAsync(request, cancellationToken);
        return Ok(alerts);
    }

    /// <summary>
    /// Consulta una alerta por su identificador.
    /// </summary>
    /// <remarks>
    /// Recomendado para refresco puntual del tablero COE cuando el frontend ya conoce el identificador de la alerta.
    /// </remarks>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AlertResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AlertResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var alert = await _alertService.GetByIdAsync(id, cancellationToken);
        return Ok(alert);
    }

    /// <summary>
    /// Crea una nueva alerta en estado Pending.
    /// </summary>
    /// <remarks>
    /// Operacion pensada para operadores del COE o integraciones internas que generan alertas iniciales antes de aprobacion manual.
    /// </remarks>
    [HttpPost]
    [Authorize(Policy = AuthPolicies.AlertCreators)]
    [ProducesResponseType(typeof(AlertResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AlertResponse>> Create(
        [FromBody] CreateAlertRequest request,
        CancellationToken cancellationToken)
    {
        var alert = await _alertService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = alert.Id, version = "1" }, alert);
    }

    /// <summary>
    /// Actualiza una alerta mientras permanezca en estado Pending.
    /// </summary>
    /// <remarks>
    /// Requiere <c>expectedVersion</c> para control de concurrencia optimista y evitar sobrescrituras accidentales.
    /// </remarks>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthPolicies.AlertOperators)]
    [ProducesResponseType(typeof(AlertResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AlertResponse>> Update(
        Guid id,
        [FromBody] UpdateAlertRequest request,
        CancellationToken cancellationToken)
    {
        var alert = await _alertService.UpdateAsync(id, request, cancellationToken);
        return Ok(alert);
    }

    /// <summary>
    /// Aprueba manualmente una alerta Pending dentro de la ventana de 3 minutos.
    /// </summary>
    /// <remarks>
    /// Si la alerta ya fue aprobada, cambiada o vencio su ventana de aprobacion, la API devuelve conflicto o violacion de regla de negocio.
    /// </remarks>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = AuthPolicies.AlertApprovers)]
    [ProducesResponseType(typeof(AlertResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AlertResponse>> Approve(
        Guid id,
        [FromBody] ApproveAlertRequest request,
        CancellationToken cancellationToken)
    {
        var alert = await _alertService.ApproveAsync(id, request, cancellationToken);
        return Ok(alert);
    }

    /// <summary>
    /// Rechaza manualmente una alerta Pending.
    /// </summary>
    /// <remarks>
    /// El motivo de rechazo es obligatorio y queda registrado en auditoria para trazabilidad operativa.
    /// </remarks>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = AuthPolicies.AlertApprovers)]
    [ProducesResponseType(typeof(AlertResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AlertResponse>> Reject(
        Guid id,
        [FromBody] RejectAlertRequest request,
        CancellationToken cancellationToken)
    {
        var alert = await _alertService.RejectAsync(id, request, cancellationToken);
        return Ok(alert);
    }

    /// <summary>
    /// Cancela una alerta y deja auditoría obligatoria de la acción.
    /// </summary>
    /// <remarks>
    /// La cancelacion no elimina la alerta; conserva el historial y evita nuevas transiciones incompatibles.
    /// </remarks>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = AuthPolicies.AlertApprovers)]
    [ProducesResponseType(typeof(AlertResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AlertResponse>> Cancel(
        Guid id,
        [FromBody] CancelAlertRequest request,
        CancellationToken cancellationToken)
    {
        var alert = await _alertService.CancelAsync(id, request, cancellationToken);
        return Ok(alert);
    }

    /// <summary>
    /// Elimina administrativamente una alerta sin borrarla fisicamente de la base de datos.
    /// </summary>
    /// <remarks>
    /// Operacion restringida a administradores. La alerta queda marcada como eliminada para no perder trazabilidad
    /// de eventos generados por otros sistemas.
    /// </remarks>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthPolicies.AdminsOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromBody] DeleteAlertRequest request,
        CancellationToken cancellationToken)
    {
        await _alertService.DeleteAsync(id, request, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Registra la difusion de una alerta aprobada o previamente broadcasted.
    /// </summary>
    /// <remarks>
    /// Este endpoint representa la salida hacia canales de notificacion o integraciones de difusion masiva.
    /// </remarks>
    [HttpPost("{id:guid}/dispatch")]
    [Authorize(Policy = AuthPolicies.Dispatchers)]
    [ProducesResponseType(typeof(AlertResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AlertResponse>> Dispatch(
        Guid id,
        [FromBody] DispatchAlertRequest request,
        CancellationToken cancellationToken)
    {
        var alert = await _alertService.DispatchAsync(id, request, cancellationToken);
        return Ok(alert);
    }

    /// <summary>
    /// Registra la confirmacion ciudadana de que una alerta corresponde a una situacion real.
    /// </summary>
    /// <remarks>
    /// Disponible para ciudadanos y operadores. Solo permite una confirmacion por usuario por alerta.
    /// La alerta debe estar Aprobada o Difundida para poder confirmarse.
    /// </remarks>
    [HttpPost("{id:guid}/citizen-confirm")]
    [Authorize(Policy = AuthPolicies.CitizenConfirmers)]
    [ProducesResponseType(typeof(CitizenConfirmationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CitizenConfirmationResponse>> CitizenConfirm(
        Guid id,
        [FromBody] CitizenConfirmAlertRequest request,
        CancellationToken cancellationToken)
    {
        var confirmation = await _alertService.CitizenConfirmAsync(id, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id, version = "1" }, confirmation);
    }

    /// <summary>
    /// Devuelve la lista de confirmaciones ciudadanas de una alerta.
    /// </summary>
    /// <remarks>
    /// Disponible para Admin, Operadores y Analistas. Permite ver quien confirmo la alerta en campo.
    /// </remarks>
    [HttpGet("{id:guid}/citizen-confirmations")]
    [Authorize(Policy = AuthPolicies.ConfirmationReaders)]
    [ProducesResponseType(typeof(CitizenConfirmationResponse[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CitizenConfirmationResponse[]>> GetCitizenConfirmations(
        Guid id,
        CancellationToken cancellationToken)
    {
        var confirmations = await _alertService.GetCitizenConfirmationsAsync(id, cancellationToken);
        return Ok(confirmations);
    }
}
