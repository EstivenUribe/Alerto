namespace Alerto.Application.Weather;

public interface IWeatherService
{
    Task<WeatherDashboardResponse> GetDashboardAsync(decimal latitude, decimal longitude, bool forceRefresh, CancellationToken cancellationToken);
    Task<WeatherHistoryResponse[]> GetHistoryAsync(decimal latitude, decimal longitude, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);
    bool IsDemoMode { get; }
    void SetThresholdMode(bool demoMode);
}
