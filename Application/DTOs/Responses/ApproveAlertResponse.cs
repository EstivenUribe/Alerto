namespace Alerto.Application.DTOs.Responses;

/// <summary>
/// DTO de respuesta para la aprobación de una alerta.
/// </summary>
public class ApproveAlertResponse
{
    public Guid Id { get; set; }
    public string Estado { get; set; } = "Aprobada";
    public int OperadorUsuarioId { get; set; }
    public string OperadorNombre { get; set; } = string.Empty;
    public DateTime TimestampAprobacion { get; set; }
    public int TiempoRespuestaSegundos { get; set; }
    public DifusionInfo Difusion { get; set; } = new();
}
