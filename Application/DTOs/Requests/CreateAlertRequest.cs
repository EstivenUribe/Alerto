using Alerto.Domain.Entities;

namespace Alerto.Application.DTOs.Requests;

/// <summary>
/// DTO para crear una nueva alerta.
/// </summary>
public class CreateAlertRequest
{
    /// <summary>Identificador único CAP de la alerta</summary>
    public required string IdentificadorCap { get; set; }

    /// <summary>Nivel de severidad (Advisory, Watch, Warning, Emergency)</summary>
    public required Severity Severidad { get; set; }

    /// <summary>Descripción breve del evento</summary>
    public required string Evento { get; set; }

    /// <summary>ID de la geocerca donde aplica la alerta</summary>
    public required int GeocercaId { get; set; }

    /// <summary>Mensaje en español (máx 1000 caracteres)</summary>
    public required string MensajeEs { get; set; }

    /// <summary>Población estimada a alertar</summary>
    public required int PoblacionObjetivo { get; set; }
}
