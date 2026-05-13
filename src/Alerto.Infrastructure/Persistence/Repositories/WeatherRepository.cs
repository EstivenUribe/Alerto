using Alerto.Application.Common.Interfaces;
using Alerto.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Alerto.Infrastructure.Persistence.Repositories;

public sealed class WeatherRepository : IWeatherRepository
{
    private readonly AlertoDbContext _dbContext;

    public WeatherRepository(AlertoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(WeatherReading reading, CancellationToken cancellationToken)
    {
        await _dbContext.WeatherReadings.AddAsync(reading, cancellationToken);
    }

    public async Task<WeatherReading[]> GetHistoryAsync(decimal latitude, decimal longitude, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        const decimal tolerance = 0.01m;

        return await _dbContext.WeatherReadings
            .Where(r =>
                Math.Abs(r.Latitude - latitude) <= tolerance &&
                Math.Abs(r.Longitude - longitude) <= tolerance &&
                r.CreatedAtUtc >= fromUtc &&
                r.CreatedAtUtc <= toUtc)
            .OrderByDescending(r => r.CreatedAtUtc)
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);
    }
}
