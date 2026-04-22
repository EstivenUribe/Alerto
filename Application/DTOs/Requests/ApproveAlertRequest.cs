namespace Alerto.Application.DTOs.Requests;

/// <summary>
/// DTO para aprobar una alerta.
/// </summary>
public class ApproveAlertRequest
{
    /// <summary>Comentario opcional del operador (máx 500 caracteres)</summary>
    public string? Comentario { get; set; }
}
