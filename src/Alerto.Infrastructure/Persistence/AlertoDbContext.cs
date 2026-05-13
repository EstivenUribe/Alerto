using Alerto.Domain.Common;
using Alerto.Domain.Entities;
using Alerto.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Alerto.Infrastructure.Persistence;

public sealed class AlertoDbContext : DbContext
{
    public AlertoDbContext(DbContextOptions<AlertoDbContext> options)
        : base(options)
    {
    }

    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<AlertDispatch> AlertDispatches => Set<AlertDispatch>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Geofence> Geofences => Set<Geofence>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<WeatherReading> WeatherReadings => Set<WeatherReading>();
    public DbSet<AlertCitizenConfirmation> AlertCitizenConfirmations => Set<AlertCitizenConfirmation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("alerto");

        // DomainEvents are in-memory only; EF must not attempt to persist them
        modelBuilder.Ignore<DomainEvent>();

        modelBuilder.Entity<Alert>(builder =>
        {
            builder.ToTable("alerts");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Title).HasMaxLength(160).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            builder.Property(x => x.SourceSystem).HasMaxLength(80).IsRequired();
            builder.Property(x => x.Address).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Latitude).HasPrecision(9, 6);
            builder.Property(x => x.Longitude).HasPrecision(9, 6);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            builder.Property(x => x.Severity).HasConversion<string>().HasMaxLength(20);
            builder.Property(x => x.RejectionReason).HasMaxLength(500);
            builder.Property(x => x.CancellationReason).HasMaxLength(500);
            builder.Property(x => x.DeletionReason).HasMaxLength(500);
            builder.Property(x => x.Version).IsConcurrencyToken();
            builder.HasIndex(x => x.IsDeleted);
            builder.HasMany(x => x.Dispatches)
                .WithOne()
                .HasForeignKey(x => x.AlertId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AlertDispatch>(builder =>
        {
            builder.ToTable("alert_dispatches");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Channel).HasConversion<string>().HasMaxLength(30);
            builder.Property(x => x.Destination).HasMaxLength(160).IsRequired();
            builder.Property(x => x.ProviderReference).HasMaxLength(160).IsRequired();
            builder.Property(x => x.Version).IsConcurrencyToken();
        });

        modelBuilder.Entity<AuditLog>(builder =>
        {
            builder.ToTable("audit_trails");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Action).HasMaxLength(80).IsRequired();
            builder.Property(x => x.EntityName).HasMaxLength(80).IsRequired();
            builder.Property(x => x.TraceId).HasMaxLength(100).IsRequired();
            builder.Property(x => x.DetailsJson).HasColumnType("jsonb");
            builder.Property(x => x.Version).IsConcurrencyToken();
        });

        modelBuilder.Entity<Geofence>(builder =>
        {
            builder.ToTable("geofences");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Code).HasMaxLength(30).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
            builder.Property(x => x.PolygonWkt).HasMaxLength(5000).IsRequired();
            builder.Property(x => x.Neighborhood).HasMaxLength(120).IsRequired();
            builder.Property(x => x.Version).IsConcurrencyToken();
            builder.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<User>(builder =>
        {
            builder.ToTable("users");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Username).HasMaxLength(80).IsRequired();
            builder.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
            builder.Property(x => x.Email).HasMaxLength(160).IsRequired();
            builder.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(30);
            builder.Property(x => x.TotpSecret).HasMaxLength(128);
            builder.Property(x => x.Version).IsConcurrencyToken();
            builder.HasIndex(x => x.Username).IsUnique();
            builder.HasIndex(x => x.Email).IsUnique();
            builder.HasMany(x => x.RefreshTokens)
                .WithOne()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(builder =>
        {
            builder.ToTable("refresh_tokens");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Token).HasMaxLength(256).IsRequired();
            builder.Property(x => x.CreatedByIp).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Version).IsConcurrencyToken();
            builder.HasIndex(x => x.Token).IsUnique();
        });

        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.ToTable("outbox_messages");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Type).HasMaxLength(40).IsRequired();
            builder.Property(x => x.CorrelationId).HasMaxLength(100).IsRequired();
            builder.Property(x => x.PayloadJson).HasColumnType("jsonb");
            builder.Property(x => x.LastError).HasMaxLength(1000);
            builder.HasIndex(x => x.ProcessedAtUtc);
            builder.HasIndex(x => x.OccurredAtUtc);
        });

        modelBuilder.Entity<AlertCitizenConfirmation>(builder =>
        {
            builder.ToTable("alert_citizen_confirmations");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Notes).HasMaxLength(500);
            builder.Property(x => x.Version).IsConcurrencyToken();
            builder.HasIndex(x => x.AlertId);
            builder.HasIndex(x => new { x.AlertId, x.ConfirmedByUserId }).IsUnique();
        });

        modelBuilder.Entity<WeatherReading>(builder =>
        {
            builder.ToTable("weather_readings");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Latitude).HasPrecision(9, 6).IsRequired();
            builder.Property(x => x.Longitude).HasPrecision(9, 6).IsRequired();
            builder.Property(x => x.PrecipitationMmPerHour).HasPrecision(7, 2).IsRequired();
            builder.Property(x => x.RiskLevel).HasConversion<string>().HasMaxLength(20);
            builder.Property(x => x.HourlyForecastJson).HasColumnType("jsonb").IsRequired();
            builder.Property(x => x.Version).IsConcurrencyToken();
            builder.HasIndex(x => x.CreatedAtUtc);
            builder.HasIndex(x => new { x.Latitude, x.Longitude, x.CreatedAtUtc });
        });
    }
}
