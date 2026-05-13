using Alerto.Domain.Common;
using Alerto.Domain.Enums;

namespace Alerto.Domain.Entities;

public sealed class WeatherReading : BaseEntity
{
    private WeatherReading()
    {
    }

    private WeatherReading(
        decimal latitude,
        decimal longitude,
        decimal precipitationMmPerHour,
        int precipitationProbabilityPercent,
        int weatherCode,
        PrecipitationRiskLevel riskLevel,
        string hourlyForecastJson,
        DateTime utcNow)
    {
        Latitude = latitude;
        Longitude = longitude;
        PrecipitationMmPerHour = precipitationMmPerHour;
        PrecipitationProbabilityPercent = precipitationProbabilityPercent;
        WeatherCode = weatherCode;
        RiskLevel = riskLevel;
        HourlyForecastJson = hourlyForecastJson;
        CreatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public decimal Latitude { get; private set; }
    public decimal Longitude { get; private set; }
    public decimal PrecipitationMmPerHour { get; private set; }
    public int PrecipitationProbabilityPercent { get; private set; }
    public int WeatherCode { get; private set; }
    public PrecipitationRiskLevel RiskLevel { get; private set; }
    public string HourlyForecastJson { get; private set; } = string.Empty;
    public bool AutoAlertCreated { get; private set; }
    public Guid? AutoAlertId { get; private set; }

    public static WeatherReading Create(
        decimal latitude,
        decimal longitude,
        decimal precipitationMmPerHour,
        int precipitationProbabilityPercent,
        int weatherCode,
        PrecipitationRiskLevel riskLevel,
        string hourlyForecastJson,
        DateTime utcNow) =>
        new(latitude, longitude, precipitationMmPerHour, precipitationProbabilityPercent,
            weatherCode, riskLevel, hourlyForecastJson, utcNow);

    public void MarkAutoAlertCreated(Guid alertId, DateTime utcNow)
    {
        AutoAlertCreated = true;
        AutoAlertId = alertId;
        Touch(utcNow);
    }
}
