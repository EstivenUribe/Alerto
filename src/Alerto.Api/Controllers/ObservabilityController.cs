using Alerto.Api.Observability;
using Microsoft.AspNetCore.Mvc;

namespace Alerto.Api.Controllers;

[ApiController]
[Route("metrics/basic")]
public sealed class ObservabilityController : ControllerBase
{
    private readonly IApiMetrics _metrics;

    public ObservabilityController(IApiMetrics metrics)
    {
        _metrics = metrics;
    }

    /// <summary>
    /// Devuelve una instantanea de metricas basicas de la API para soporte operativo y evaluacion tecnica.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(MetricsSnapshot), StatusCodes.Status200OK)]
    public ActionResult<MetricsSnapshot> GetMetrics()
    {
        return Ok(_metrics.GetSnapshot());
    }
}
