using System.Text.Json;
using Alerto.Application.Common.Interfaces;
using Alerto.Domain.Entities;
using Alerto.Domain.Enums;

namespace Alerto.Application.Weather;

public sealed class WeatherService : IWeatherService
{
    private const string CachePrefix = "weather:";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private static readonly Dictionary<int, string> WmoDescriptions = new()
    {
        { 0, "Cielo despejado" }, { 1, "Principalmente despejado" }, { 2, "Parcialmente nublado" }, { 3, "Nublado" },
        { 45, "Neblina" }, { 48, "Escarcha" },
        { 51, "Llovizna ligera" }, { 53, "Llovizna moderada" }, { 55, "Llovizna densa" },
        { 61, "Lluvia ligera" }, { 63, "Lluvia moderada" }, { 65, "Lluvia intensa" },
        { 71, "Nieve ligera" }, { 73, "Nieve moderada" }, { 75, "Nieve intensa" },
        { 80, "Chubascos ligeros" }, { 81, "Chubascos moderados" }, { 82, "Chubascos violentos" },
        { 95, "Tormenta eléctrica" }, { 96, "Tormenta con granizo ligero" }, { 99, "Tormenta con granizo fuerte" }
    };

    private readonly IWeatherRepository _weatherRepository;
    private readonly IAlertRepository _alertRepository;
    private readonly IGeofenceRepository _geofenceRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOpenMeteoClient _openMeteoClient;
    private readonly IAppCache _cache;
    private readonly IClock _clock;
    private readonly IWeatherThresholdStore _thresholdStore;

    public bool IsDemoMode => _thresholdStore.IsDemoMode;

    public void SetThresholdMode(bool demoMode) => _thresholdStore.SetMode(demoMode);

    public WeatherService(
        IWeatherRepository weatherRepository,
        IAlertRepository alertRepository,
        IGeofenceRepository geofenceRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IOpenMeteoClient openMeteoClient,
        IAppCache cache,
        IClock clock,
        IWeatherThresholdStore thresholdStore)
    {
        _weatherRepository = weatherRepository;
        _alertRepository = alertRepository;
        _geofenceRepository = geofenceRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _openMeteoClient = openMeteoClient;
        _cache = cache;
        _clock = clock;
        _thresholdStore = thresholdStore;
    }

    public async Task<WeatherDashboardResponse> GetDashboardAsync(decimal latitude, decimal longitude, bool forceRefresh, CancellationToken cancellationToken)
    {
        var cacheKey = BuildCacheKey(latitude, longitude);
        if (!forceRefresh)
        {
            var cached = await _cache.GetAsync<WeatherDashboardResponse>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                return cached with { IsFromCache = true };
            }
        }

        var data = await _openMeteoClient.GetForecastAsync(latitude, longitude, cancellationToken);
        var riskLevel = _thresholdStore.IsDemoMode
            ? ComputeRiskLevelDemo(data.CurrentPrecipitationMm, data.CurrentProbabilityPercent)
            : ComputeRiskLevel(data.CurrentPrecipitationMm, data.CurrentProbabilityPercent, data.CurrentWeatherCode);
        var forecastJson = JsonSerializer.Serialize(data.HourlySlots);

        var reading = WeatherReading.Create(
            latitude,
            longitude,
            data.CurrentPrecipitationMm,
            data.CurrentProbabilityPercent,
            data.CurrentWeatherCode,
            riskLevel,
            forecastJson,
            _clock.UtcNow);

        await _weatherRepository.AddAsync(reading, cancellationToken);

        if (riskLevel >= PrecipitationRiskLevel.High)
        {
            var alertId = await TryCreateAutoAlertAsync(reading, riskLevel, cancellationToken);
            if (alertId.HasValue)
            {
                reading.MarkAutoAlertCreated(alertId.Value, _clock.UtcNow);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var response = BuildDashboardResponse(reading, data, isFromCache: false);
        await _cache.SetAsync(cacheKey, response, CacheTtl, cancellationToken);
        return response;
    }

    public async Task<WeatherHistoryResponse[]> GetHistoryAsync(decimal latitude, decimal longitude, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        var readings = await _weatherRepository.GetHistoryAsync(latitude, longitude, fromUtc, toUtc, cancellationToken);
        return readings.Select(r => new WeatherHistoryResponse(
            r.Id,
            r.Latitude,
            r.Longitude,
            r.PrecipitationMmPerHour,
            r.PrecipitationProbabilityPercent,
            r.WeatherCode,
            DescribeWmo(r.WeatherCode),
            DescribeRiskLevel(r.RiskLevel),
            r.AutoAlertCreated,
            r.AutoAlertId,
            r.CreatedAtUtc)).ToArray();
    }

    private async Task<Guid?> TryCreateAutoAlertAsync(WeatherReading reading, PrecipitationRiskLevel riskLevel, CancellationToken cancellationToken)
    {
        var geofence = await _geofenceRepository.GetFirstActiveAsync(cancellationToken);
        if (geofence is null)
        {
            return null;
        }

        var adminUser = await _userRepository.GetFirstAdminAsync(cancellationToken);
        if (adminUser is null)
        {
            return null;
        }

        var severity = riskLevel == PrecipitationRiskLevel.Critical ? Severity.Critical : Severity.Severe;
        var title = riskLevel == PrecipitationRiskLevel.Critical
            ? "Riesgo crítico de precipitación detectado"
            : "Riesgo alto de precipitación detectado";
        var description =
            $"Open-Meteo reporta {reading.PrecipitationMmPerHour:F1} mm/h de precipitación " +
            $"con {reading.PrecipitationProbabilityPercent}% de probabilidad en coordenadas " +
            $"({reading.Latitude:F4}, {reading.Longitude:F4}). Nivel de riesgo: {DescribeRiskLevel(riskLevel)}.";

        var alert = Alert.Create(
            title,
            description,
            severity,
            "OPEN_METEO",
            $"Lat: {reading.Latitude:F4}, Lon: {reading.Longitude:F4}",
            reading.Latitude,
            reading.Longitude,
            geofence.Id,
            adminUser.Id,
            _clock.UtcNow);

        await _alertRepository.AddAsync(alert, cancellationToken);
        return alert.Id;
    }

    // Umbrales ultra-sensibles para demostración: disparan auto-alerta con condiciones normales de Medellín
    private static PrecipitationRiskLevel ComputeRiskLevelDemo(decimal precipMm, int probability)
    {
        if (precipMm >= 0.5m || probability >= 75) return PrecipitationRiskLevel.Critical;
        if (precipMm >= 0.1m || probability >= 60) return PrecipitationRiskLevel.High;
        if (precipMm >  0m   || probability >= 40) return PrecipitationRiskLevel.Moderate;
        return PrecipitationRiskLevel.Low;
    }

    private static PrecipitationRiskLevel ComputeRiskLevel(decimal precipMm, int probability, int weatherCode)
    {
        // WMO codes 95/96/99 = tormenta eléctrica → mínimo High aunque no llueva
        var isThunderstorm = weatherCode is 95 or 96 or 99;

        // Sin precipitación actual: la probabilidad no puede generar Critical/High por sí sola
        if (precipMm < 0.1m)
            return isThunderstorm ? PrecipitationRiskLevel.High : PrecipitationRiskLevel.Low;

        // Precipitación real: señal primaria.
        // La probabilidad puede subir UN nivel adicional como máximo.
        if (precipMm >= 15m) return PrecipitationRiskLevel.Critical;
        if (precipMm >= 7.5m) return probability >= 70 ? PrecipitationRiskLevel.Critical : PrecipitationRiskLevel.High;
        if (precipMm >= 2.5m) return probability >= 70 ? PrecipitationRiskLevel.High     : PrecipitationRiskLevel.Moderate;

        // Lluvia ligera (0.1–2.4 mm/h)
        if (isThunderstorm) return PrecipitationRiskLevel.Moderate;
        return probability >= 75 ? PrecipitationRiskLevel.Moderate : PrecipitationRiskLevel.Low;
    }

    private static WeatherDashboardResponse BuildDashboardResponse(WeatherReading reading, OpenMeteoForecastData data, bool isFromCache)
    {
        var forecast = data.HourlySlots.Select(h => new HourlyForecastPoint(
            h.TimeUtc,
            h.PrecipitationMm,
            h.ProbabilityPercent,
            h.WeatherCode,
            DescribeWmo(h.WeatherCode))).ToArray();

        return new WeatherDashboardResponse(
            reading.Latitude,
            reading.Longitude,
            reading.PrecipitationMmPerHour,
            reading.PrecipitationProbabilityPercent,
            reading.WeatherCode,
            DescribeWmo(reading.WeatherCode),
            DescribeRiskLevel(reading.RiskLevel),
            reading.AutoAlertCreated,
            reading.AutoAlertId,
            reading.CreatedAtUtc,
            isFromCache,
            forecast);
    }

    private static string BuildCacheKey(decimal lat, decimal lon) =>
        $"{CachePrefix}{lat:F4}:{lon:F4}";

    private static string DescribeWmo(int code) =>
        WmoDescriptions.TryGetValue(code, out var desc) ? desc : "Condición meteorológica";

    private static string DescribeRiskLevel(PrecipitationRiskLevel level) => level switch
    {
        PrecipitationRiskLevel.Low      => "Bajo",
        PrecipitationRiskLevel.Moderate => "Moderado",
        PrecipitationRiskLevel.High     => "Alto",
        PrecipitationRiskLevel.Critical => "Crítico",
        _                               => level.ToString()
    };
}
