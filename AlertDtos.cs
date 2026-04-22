using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Alerto.Application.DTOs.Alerts;

// ═══════════════════════════════════════════════════════
// REQUEST DTOs
// ═══════════════════════════════════════════════════════

/// <summary>
/// DTO para crear una nueva alerta (POST /api/v1/alerts).
/// Validaciones con DataAnnotations + FluentValidation.
/// </summary>
public record CreateAlertRequest
{
    [Required(ErrorMessage = "El identificador CAP es obligatorio")]
    [RegularExpression(@"^urn:oid:[\d.]+$", ErrorMessage = "Formato CAP inválido")]
    [JsonPropertyName("identificadorCap")]
    public string IdentificadorCap { get; init; } = default!;

    [Required(ErrorMessage = "La severidad es obligatoria")]
    [JsonPropertyName("severidad")]
    public string Severidad { get; init; } = default!;

    [Required(ErrorMessage = "El evento es obligatorio")]
    [MinLength(10, ErrorMessage = "El evento debe tener al menos 10 caracteres")]
    [MaxLength(255)]
    [JsonPropertyName("evento")]
    public string Evento { get; init; } = default!;

    [Required(ErrorMessage = "La geocerca es obligatoria")]
    [Range(1, int.MaxValue, ErrorMessage = "ID de geocerca inválido")]
    [JsonPropertyName("geocercaId")]
    public int GeocercaId { get; init; }

    [Required(ErrorMessage = "El mensaje es obligatorio")]
    [MinLength(20, ErrorMessage = "El mensaje debe tener al menos 20 caracteres")]
    [MaxLength(1000)]
    [JsonPropertyName("mensajeEs")]
    public string MensajeEs { get; init; } = default!;

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "La población debe ser mayor a 0")]
    [JsonPropertyName("poblacionObjetivo")]
    public int PoblacionObjetivo { get; init; }
}

/// <summary>
/// DTO para aprobar una alerta (POST /api/v1/alerts/{id}/approve).
/// </summary>
public record ApproveAlertRequest
{
    [MaxLength(500)]
    [JsonPropertyName("comentario")]
    public string? Comentario { get; init; }
}

/// <summary>
/// DTO para rechazar una alerta (POST /api/v1/alerts/{id}/reject).
/// </summary>
public record RejectAlertRequest
{
    [Required(ErrorMessage = "La justificación es obligatoria al rechazar")]
    [MinLength(10)]
    [MaxLength(500)]
    [JsonPropertyName("justificacion")]
    public string Justificacion { get; init; } = default!;
}

// ═══════════════════════════════════════════════════════
// RESPONSE DTOs
// ═══════════════════════════════════════════════════════

/// <summary>
/// Respuesta estándar de una alerta.
/// </summary>
public record AlertResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("identificadorCap")]
    public string IdentificadorCap { get; init; } = default!;

    [JsonPropertyName("severidad")]
    public string Severidad { get; init; } = default!;

    [JsonPropertyName("evento")]
    public string Evento { get; init; } = default!;

    [JsonPropertyName("geocercaId")]
    public int GeocercaId { get; init; }

    [JsonPropertyName("geocercaNombre")]
    public string GeocercaNombre { get; init; } = default!;

    [JsonPropertyName("mensajeEs")]
    public string MensajeEs { get; init; } = default!;

    [JsonPropertyName("estado")]
    public string Estado { get; init; } = default!;

    [JsonPropertyName("confianzaScore")]
    public int ConfianzaScore { get; init; }

    [JsonPropertyName("operadorUsuarioId")]
    public int? OperadorUsuarioId { get; init; }

    [JsonPropertyName("timestampGeneracion")]
    public DateTime TimestampGeneracion { get; init; }

    [JsonPropertyName("timestampDifusion")]
    public DateTime? TimestampDifusion { get; init; }

    [JsonPropertyName("poblacionObjetivo")]
    public int PoblacionObjetivo { get; init; }

    [JsonPropertyName("poblacionAlcanzada")]
    public int PoblacionAlcanzada { get; init; }
}

/// <summary>
/// Respuesta de aprobación con información de difusión.
/// </summary>
public record ApproveAlertResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("estado")]
    public string Estado { get; init; } = "Aprobada";

    [JsonPropertyName("operadorUsuarioId")]
    public int OperadorUsuarioId { get; init; }

    [JsonPropertyName("operadorNombre")]
    public string OperadorNombre { get; init; } = default!;

    [JsonPropertyName("timestampAprobacion")]
    public DateTime TimestampAprobacion { get; init; }

    [JsonPropertyName("tiempoRespuestaSegundos")]
    public int TiempoRespuestaSegundos { get; init; }

    [JsonPropertyName("difusion")]
    public DifusionInfo Difusion { get; init; } = default!;
}

/// <summary>
/// Información del estado de difusión tras aprobación.
/// </summary>
public record DifusionInfo
{
    [JsonPropertyName("estado")]
    public string Estado { get; init; } = default!;

    [JsonPropertyName("capMessageId")]
    public string CapMessageId { get; init; } = default!;

    [JsonPropertyName("canales")]
    public List<string> Canales { get; init; } = new();

    [JsonPropertyName("poblacionObjetivo")]
    public int PoblacionObjetivo { get; init; }
}
