using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alerto.Domain.Entities;

/// <summary>
/// Entidad de dominio: Alerta civil georreferenciada.
/// Contiene la lógica de transiciones de estado y reglas de negocio.
/// </summary>
[Table("alertas")]
public class Alert
{
    [Key]
    [Column("id")]
    public Guid Id { get; private set; } = Guid.NewGuid();

    [Column("identificador_cap")]
    [Required, MaxLength(255)]
    public string IdentificadorCap { get; set; } = default!;

    [Column("severidad")]
    [Required]
    public Severity Severidad { get; set; }

    [Column("evento")]
    [Required, MaxLength(255)]
    public string Evento { get; set; } = default!;

    [Column("geocerca_id")]
    public int GeocercaId { get; set; }

    [Column("mensaje_es")]
    [Required]
    public string MensajeEs { get; set; } = default!;

    [Column("estado")]
    public AlertStatus Estado { get; private set; } = AlertStatus.Pendiente;

    [Column("confianza_score")]
    [Range(0, 100)]
    public int ConfianzaScore { get; set; }

    [Column("operador_usuario_id")]
    public int? OperadorUsuarioId { get; private set; }

    [Column("timestamp_generacion")]
    public DateTime TimestampGeneracion { get; private set; } = DateTime.UtcNow;

    [Column("timestamp_difusion")]
    public DateTime? TimestampDifusion { get; private set; }

    [Column("poblacion_objetivo")]
    public int PoblacionObjetivo { get; set; }

    [Column("poblacion_alcanzada")]
    public int PoblacionAlcanzada { get; set; }

    // ── Navegación ──
    [ForeignKey(nameof(GeocercaId))]
    public virtual Geocerca? Geocerca { get; set; }

    [ForeignKey(nameof(OperadorUsuarioId))]
    public virtual Usuario? Operador { get; set; }

    // ══════════════════════════════════════════════════
    // REGLAS DE NEGOCIO
    // ══════════════════════════════════════════════════

    private static readonly TimeSpan TimeoutAprobacion = TimeSpan.FromMinutes(3);

    /// <summary>
    /// Aprueba la alerta para difusión. Valida reglas de negocio.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Si la alerta no está en estado Pendiente o el timeout ha sido excedido.
    /// </exception>
    public void Aprobar(int operadorId)
    {
        if (Estado != AlertStatus.Pendiente)
            throw new InvalidOperationException(
                $"Solo alertas en estado Pendiente pueden ser aprobadas. Estado actual: {Estado}");

        var tiempoTranscurrido = DateTime.UtcNow - TimestampGeneracion;
        if (tiempoTranscurrido > TimeoutAprobacion)
            throw new BusinessRuleException(
                $"Tiempo de aprobación excedido ({tiempoTranscurrido.TotalSeconds:F0}s > {TimeoutAprobacion.TotalSeconds}s). " +
                "La alerta debe ser re-evaluada.");

        Estado = AlertStatus.Aprobada;
        OperadorUsuarioId = operadorId;
    }

    /// <summary>
    /// Rechaza la alerta con justificación obligatoria.
    /// </summary>
    public void Rechazar(int operadorId, string justificacion)
    {
        if (Estado != AlertStatus.Pendiente)
            throw new InvalidOperationException(
                $"Solo alertas en estado Pendiente pueden ser rechazadas. Estado actual: {Estado}");

        if (string.IsNullOrWhiteSpace(justificacion))
            throw new ArgumentException("La justificación es obligatoria al rechazar.");

        Estado = AlertStatus.Rechazada;
        OperadorUsuarioId = operadorId;
    }

    /// <summary>
    /// Marca la alerta como difundida tras envío exitoso por Cell Broadcast.
    /// </summary>
    public void MarcarDifundida(int poblacionAlcanzada)
    {
        if (Estado != AlertStatus.Aprobada)
            throw new InvalidOperationException("Solo alertas aprobadas pueden marcarse como difundidas.");

        Estado = AlertStatus.Difundida;
        TimestampDifusion = DateTime.UtcNow;
        PoblacionAlcanzada = poblacionAlcanzada;
    }

    /// <summary>
    /// Cancela la alerta y envía mensaje de "todo claro".
    /// </summary>
    public void Cancelar(int operadorId)
    {
        if (Estado != AlertStatus.Aprobada && Estado != AlertStatus.Difundida)
            throw new InvalidOperationException(
                "Solo alertas aprobadas o difundidas pueden ser canceladas.");

        Estado = AlertStatus.Cancelada;
        OperadorUsuarioId = operadorId;
    }

    /// <summary>
    /// Calcula el tiempo de respuesta entre creación y aprobación.
    /// </summary>
    public int ObtenerTiempoRespuestaSegundos()
    {
        if (Estado == AlertStatus.Pendiente) return 0;
        return (int)(DateTime.UtcNow - TimestampGeneracion).TotalSeconds;
    }
}

// ── Enums ──

public enum AlertStatus
{
    Pendiente,
    Aprobada,
    Rechazada,
    Difundida,
    Cancelada
}

public enum Severity
{
    Advisory,
    Watch,
    Warning,
    Emergency
}

// ── Excepciones de dominio ──

public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message) { }
}
