using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Alerto.Infrastructure.Persistence;

internal sealed class AlertoDbContextFactory : IDesignTimeDbContextFactory<AlertoDbContext>
{
    public AlertoDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "Alerto.Api"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<AlertoDbContext>();
        optionsBuilder.UseNpgsql(configuration.GetConnectionString("AlertoDb"));

        return new AlertoDbContext(optionsBuilder.Options);
    }
}
