namespace Alerto.Application.DTOs.Requests;

/// <summary>
/// DTO para rechazar una alerta.
/// </summary>
public class RejectAlertRequest
{
    /// <summary>Justificación del rechazo (requerida, 10-500 caracteres)</summary>
    public required string Justificacion { get; set; }
}
