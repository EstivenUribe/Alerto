using Alerto.Api.Security;
using Alerto.Application.Weather;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alerto.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/weather")]
[Authorize(Policy = AuthPolicies.AlertReaders)]
public sealed class WeatherController : ControllerBase
{
    private readonly IWeatherService _weatherService;

    public WeatherController(IWeatherService weatherService)
    {
        _weatherService = weatherService;
    }

    /// <summary>
    /// Obtiene el dashboard meteorologico actual para las coordenadas del usuario.
    /// </summary>
    /// <remarks>
    /// Consulta Open-Meteo para las coordenadas indicadas. Los datos se cachean 5 minutos.
    /// Si el nivel de riesgo es Alto o Critico, se crea automaticamente una alerta en el sistema.
    /// </remarks>
    /// <param name="latitude">Latitud del usuario (entre -90 y 90).</param>
    /// <param name="longitude">Longitud del usuario (entre -180 y 180).</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(WeatherDashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<WeatherDashboardResponse>> GetDashboard(
        [FromQuery] decimal latitude,
        [FromQuery] decimal longitude,
        [FromQuery] bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        if (latitude < -90 || latitude > 90)
            return BadRequest(new ProblemDetails { Title = "Latitud invalida.", Detail = "La latitud debe estar entre -90 y 90." });

        if (longitude < -180 || longitude > 180)
            return BadRequest(new ProblemDetails { Title = "Longitud invalida.", Detail = "La longitud debe estar entre -180 y 180." });

        var result = await _weatherService.GetDashboardAsync(latitude, longitude, forceRefresh, cancellationToken);
        return Ok(result);
    }

    /// <summary>Obtiene el modo de umbrales de riesgo activo (Admin).</summary>
    [HttpGet("threshold-mode")]
    [Authorize(Policy = AuthPolicies.AdminsOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult GetThresholdMode()
        => Ok(new { isDemoMode = _weatherService.IsDemoMode });

    /// <summary>Cambia el modo de umbrales de riesgo entre original y demo (Admin).</summary>
    [HttpPost("threshold-mode")]
    [Authorize(Policy = AuthPolicies.AdminsOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult SetThresholdMode([FromBody] SetThresholdModeRequest request)
    {
        _weatherService.SetThresholdMode(request.DemoMode);
        return NoContent();
    }

    public sealed record SetThresholdModeRequest(bool DemoMode);

    /// <summary>
    /// Consulta el historial de lecturas meteorologicas almacenadas para unas coordenadas.
    /// </summary>
    /// <param name="latitude">Latitud de referencia.</param>
    /// <param name="longitude">Longitud de referencia.</param>
    /// <param name="fromUtc">Inicio del rango (UTC). Por defecto: ultimas 24 horas.</param>
    /// <param name="toUtc">Fin del rango (UTC). Por defecto: ahora.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    [HttpGet("history")]
    [ProducesResponseType(typeof(WeatherHistoryResponse[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<WeatherHistoryResponse[]>> GetHistory(
        [FromQuery] decimal latitude,
        [FromQuery] decimal longitude,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        CancellationToken cancellationToken)
    {
        var from = fromUtc ?? DateTime.UtcNow.AddHours(-24);
        var to = toUtc ?? DateTime.UtcNow;

        if (from >= to)
        {
            return BadRequest(new ProblemDetails { Title = "Rango invalido.", Detail = "fromUtc debe ser anterior a toUtc." });
        }

        var result = await _weatherService.GetHistoryAsync(latitude, longitude, from, to, cancellationToken);
        return Ok(result);
    }
}
