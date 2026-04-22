using Alerto.Domain.Entities;

namespace Alerto.Application.DTOs.Responses;

/// <summary>
/// DTO de respuesta para una alerta.
/// </summary>
public class AlertResponse
{
    public Guid Id { get; set; }
    public string IdentificadorCap { get; set; } = string.Empty;
    public Severity Severidad { get; set; }
    public string Evento { get; set; } = string.Empty;
    public int GeocercaId { get; set; }
    public string GeocercaNombre { get; set; } = string.Empty;
    public string MensajeEs { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public int ConfianzaScore { get; set; }
    public int? OperadorUsuarioId { get; set; }
    public string? OperadorNombre { get; set; }
    public DateTime TimestampGeneracion { get; set; }
    public DateTime? TimestampDifusion { get; set; }
    public int PoblacionObjetivo { get; set; }
    public int PoblacionAlcanzada { get; set; }
}
