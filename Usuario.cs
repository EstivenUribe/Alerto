using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alerto.Domain.Entities;

/// <summary>
/// Entidad de dominio: Usuario del sistema.
/// Representa operadores, administradores y auditores.
/// </summary>
[Table("usuarios")]
public class Usuario
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("nombre")]
    [Required, MaxLength(255)]
    public string Nombre { get; set; } = default!;

    [Column("email")]
    [Required, MaxLength(255)]
    public string Email { get; set; } = default!;

    [Column("rol")]
    [Required, MaxLength(50)]
    public string Rol { get; set; } = default!;

    [Column("activo")]
    public bool Activo { get; set; } = true;

    // Navegación
    public virtual ICollection<Alert> AlertasOperadas { get; set; } = new List<Alert>();
}
