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

        if (!await _dbContext.Users.AnyAsync(cancellationToken))
        {
            var admin = User.Create(
                _bootstrapAdminOptions.Username,
                _bootstrapAdminOptions.DisplayName,
                _bootstrapAdminOptions.Email,
                UserRole.Admin,
                _passwordHasher.HashPassword(_bootstrapAdminOptions.Password),
                DateTime.UtcNow);

            await _dbContext.Users.AddAsync(admin, cancellationToken);
        }

        if (!await _dbContext.Geofences.AnyAsync(cancellationToken))
        {
            var geofence = Geofence.Create(
                "MED-CENTRO",
                "Centro Medellin",
                "POLYGON((-75.575 6.250,-75.565 6.250,-75.565 6.260,-75.575 6.260,-75.575 6.250))",
                "La Candelaria",
                DateTime.UtcNow);

            await _dbContext.Geofences.AddAsync(geofence, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Base de datos inicializada y datos semilla verificados.");
    }
}
