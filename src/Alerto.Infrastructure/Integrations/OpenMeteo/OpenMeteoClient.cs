using System.Text.Json;
using Alerto.Application.Common.Exceptions;
using Alerto.Application.Common.Interfaces;
using Alerto.Application.Weather;
using Alerto.Infrastructure.Integrations.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace Alerto.Infrastructure.Integrations.OpenMeteo;

public sealed class OpenMeteoClient : IOpenMeteoClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly OpenMeteoOptions _options;
    private readonly IAsyncPolicy<HttpResponseMessage> _policy;

    public OpenMeteoClient(
        HttpClient httpClient,
        IOptions<OpenMeteoOptions> options,
        ILogger<OpenMeteoClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _policy = HttpResiliencePolicyFactory.Create(
            "OpenMeteo",
            _options.TimeoutSeconds,
            _options.RetryCount,
            _options.CircuitBreakerFailures,
            _options.CircuitBreakerBreakSeconds,
            logger);
    }

    public async Task<OpenMeteoForecastData> GetForecastAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken)
    {
        var url = BuildUrl(latitude, longitude);

        HttpResponseMessage response;
        try
        {
            response = await _policy.ExecuteAsync(
                token => _httpClient.GetAsync(url, token),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not ExternalDependencyException)
        {
            throw new ExternalDependencyException("Open-Meteo", "No fue posible conectar con el servicio meteorológico.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new ExternalDependencyException("Open-Meteo", $"Respuesta HTTP {(int)response.StatusCode}.");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseResponse(json);
    }

    private static string BuildUrl(decimal latitude, decimal longitude)
    {
        var lat = latitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
        var lon = longitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
        return $"v1/forecast?latitude={lat}&longitude={lon}" +
               "&current=precipitation,weather_code" +
               "&hourly=precipitation,precipitation_probability,weather_code" +
               "&forecast_days=1" +
               "&timezone=UTC";
    }

    private static OpenMeteoForecastData ParseResponse(string json)
    {
        var apiResponse = JsonSerializer.Deserialize<OpenMeteoApiResponse>(json, JsonOptions)
            ?? throw new ExternalDependencyException("Open-Meteo", "La respuesta no pudo ser deserializada.");

        var currentPrecip = (decimal)(apiResponse.Current?.Precipitation ?? 0);
        var currentWeatherCode = apiResponse.Current?.WeatherCode ?? 0;

        var hourlySlots = BuildHourlySlots(apiResponse.Hourly);

        // Tomar la probabilidad del slot horario más cercano a la hora actual UTC
        var currentProbability = hourlySlots.Length > 0
            ? GetCurrentHourProbability(hourlySlots)
            : 0;

        return new OpenMeteoForecastData
        {
            CurrentPrecipitationMm = currentPrecip,
            CurrentProbabilityPercent = currentProbability,
            CurrentWeatherCode = currentWeatherCode,
            HourlySlots = hourlySlots
        };
    }

    private static HourlyForecastRaw[] BuildHourlySlots(OpenMeteoHourlyBlock? hourly)
    {
        if (hourly?.Time is null) return [];

        var count = hourly.Time.Length;
        var slots = new List<HourlyForecastRaw>(count);

        for (var i = 0; i < count; i++)
        {
            if (!DateTime.TryParse(hourly.Time[i], out var time))
            {
                continue;
            }

            slots.Add(new HourlyForecastRaw
            {
                TimeUtc = DateTime.SpecifyKind(time, DateTimeKind.Utc),
                PrecipitationMm = (decimal)(hourly.Precipitation?.ElementAtOrDefault(i) ?? 0),
                ProbabilityPercent = hourly.PrecipitationProbability?.ElementAtOrDefault(i) ?? 0,
                WeatherCode = hourly.WeatherCode?.ElementAtOrDefault(i) ?? 0
            });
        }

        return [.. slots];
    }

    private static int GetCurrentHourProbability(HourlyForecastRaw[] slots)
    {
        var now = DateTime.UtcNow;
        var closest = slots.MinBy(s => Math.Abs((s.TimeUtc - now).TotalMinutes));
        return closest?.ProbabilityPercent ?? 0;
    }
}
