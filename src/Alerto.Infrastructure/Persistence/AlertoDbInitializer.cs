using Alerto.Domain.Entities;
using Alerto.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Alerto.Infrastructure.Persistence;

public sealed class AlertoDbInitializer
{
    private readonly AlertoDbContext _dbContext;
    private readonly Authentication.BootstrapAdminOptions _bootstrapAdminOptions;
    private readonly Application.Common.Interfaces.IPasswordHasher _passwordHasher;
    private readonly ILogger<AlertoDbInitializer> _logger;

    public AlertoDbInitializer(
        AlertoDbContext dbContext,
        IOptions<Authentication.BootstrapAdminOptions> bootstrapAdminOptions,
        Application.Common.Interfaces.IPasswordHasher passwordHasher,
        ILogger<AlertoDbInitializer> logger)
    {
        _dbContext = dbContext;
        _bootstrapAdminOptions = bootstrapAdminOptions.Value;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.MigrateAsync(cancellationToken);

        var now = DateTime.UtcNow;

        if (!await _dbContext.Users.AnyAsync(cancellationToken))
        {
            var admin = User.Create(
                _bootstrapAdminOptions.Username,
                _bootstrapAdminOptions.DisplayName,
                _bootstrapAdminOptions.Email,
                UserRole.Admin,
                _passwordHasher.HashPassword(_bootstrapAdminOptions.Password),
                now);

            await _dbContext.Users.AddAsync(admin, cancellationToken);
        }

        var existingUsernames = await _dbContext.Users
            .Select(u => u.Username.ToLower())
            .ToListAsync(cancellationToken);

        if (!existingUsernames.Contains("operador"))
        {
            var operador = User.Create(
                "operador",
                "Operador Demo",
                "operador@alerto.local",
                UserRole.Operator,
                _passwordHasher.HashPassword("Alerto2026!"),
                now);

            await _dbContext.Users.AddAsync(operador, cancellationToken);
        }

        if (!existingUsernames.Contains("ciudadano"))
        {
            var ciudadano = User.Create(
                "ciudadano",
                "Ciudadano Demo",
                "ciudadano@alerto.local",
                UserRole.Citizen,
                _passwordHasher.HashPassword("Alerto2026!"),
                now);

            await _dbContext.Users.AddAsync(ciudadano, cancellationToken);
        }

        if (!await _dbContext.Geofences.AnyAsync(cancellationToken))
        {
            var geofence = Geofence.Create(
                "MED-CENTRO",
                "Centro Medellin",
                "POLYGON((-75.575 6.250,-75.565 6.250,-75.565 6.260,-75.575 6.260,-75.575 6.250))",
                "La Candelaria",
                now);

            await _dbContext.Geofences.AddAsync(geofence, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Base de datos inicializada y datos semilla verificados.");
    }
}
