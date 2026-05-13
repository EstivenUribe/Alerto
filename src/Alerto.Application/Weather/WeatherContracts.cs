namespace Alerto.Application.Weather;

public sealed record WeatherDashboardResponse(
    decimal Latitude,
    decimal Longitude,
    decimal PrecipitationMmPerHour,
    int PrecipitationProbabilityPercent,
    int WeatherCode,
    string WeatherDescription,
    string RiskLevel,
    bool AutoAlertCreated,
    Guid? AutoAlertId,
    DateTime RecordedAtUtc,
    bool IsFromCache,
    HourlyForecastPoint[] HourlyForecast);

public sealed record HourlyForecastPoint(
    DateTime TimeUtc,
    decimal PrecipitationMm,
    int PrecipitationProbabilityPercent,
    int WeatherCode,
    string WeatherDescription);

public sealed record WeatherHistoryResponse(
    Guid Id,
    decimal Latitude,
    decimal Longitude,
    decimal PrecipitationMmPerHour,
    int PrecipitationProbabilityPercent,
    int WeatherCode,
    string WeatherDescription,
    string RiskLevel,
    bool AutoAlertCreated,
    Guid? AutoAlertId,
    DateTime RecordedAtUtc);

/// <summary>
/// Datos normalizados devueltos por el cliente Open-Meteo antes de ser persistidos.
/// </summary>
public sealed class OpenMeteoForecastData
{
    public decimal CurrentPrecipitationMm { get; init; }
    public int CurrentProbabilityPercent { get; init; }
    public int CurrentWeatherCode { get; init; }
    public HourlyForecastRaw[] HourlySlots { get; init; } = [];
}

public sealed class HourlyForecastRaw
{
    public DateTime TimeUtc { get; init; }
    public decimal PrecipitationMm { get; init; }
    public int ProbabilityPercent { get; init; }
    public int WeatherCode { get; init; }
}
