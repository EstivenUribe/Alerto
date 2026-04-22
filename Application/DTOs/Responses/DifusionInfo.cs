namespace Alerto.Application.DTOs.Responses;

/// <summary>
/// Información de difusión de una alerta aprobada.
/// </summary>
public class DifusionInfo
{
    public string Estado { get; set; } = string.Empty;
    public string? CapMessageId { get; set; }
    public List<string> Canales { get; set; } = new();
    public int PoblacionObjetivo { get; set; }
}
