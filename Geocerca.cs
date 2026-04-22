using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alerto.Domain.Entities;

/// <summary>
/// Entidad de dominio: Geocerca georreferenciada.
/// Define una zona geográfica para alertas.
/// </summary>
[Table("geocercas")]
public class Geocerca
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("nombre")]
    [Required, MaxLength(255)]
    public string Nombre { get; set; } = default!;

    [Column("descripcion")]
    public string? Descripcion { get; set; }

    [Column("poblacion_estimada")]
    public int PoblacionEstimada { get; set; }

    [Column("activa")]
    public bool Activa { get; set; } = true;

    // Navegación
    public virtual ICollection<Alert> Alertas { get; set; } = new List<Alert>();
}
