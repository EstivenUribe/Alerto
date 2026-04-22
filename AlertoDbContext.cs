using Microsoft.EntityFrameworkCore;
using Alerto.Domain.Entities;

namespace Alerto.Infrastructure.Persistence;

/// <summary>
/// Contexto de base de datos para el sistema Alerto.
/// Configura entidades y relaciones usando EF Core con PostgreSQL.
/// </summary>
public class AlertoDbContext : DbContext
{
    public AlertoDbContext(DbContextOptions<AlertoDbContext> options) : base(options) { }

    public DbSet<Alert> Alertas { get; set; } = null!;
    public DbSet<Geocerca> Geocercas { get; set; } = null!;
    public DbSet<Usuario> Usuarios { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuración de la entidad Alert
        modelBuilder.Entity<Alert>(entity =>
        {
            entity.ToTable("alertas");
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.Geocerca)
                .WithMany(g => g.Alertas)
                .HasForeignKey(e => e.GeocercaId);
                
            entity.HasOne(e => e.Operador)
                .WithMany(u => u.AlertasOperadas)
                .HasForeignKey(e => e.OperadorUsuarioId);
        });

        // Configuración de la entidad Geocerca
        modelBuilder.Entity<Geocerca>(entity =>
        {
            entity.ToTable("geocercas");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).HasMaxLength(255).IsRequired();
        });

        // Configuración de la entidad Usuario
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("usuarios");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Rol).HasMaxLength(50).IsRequired();
        });
    }
}
