using System.Text.Json.Serialization;

namespace Alerto.Infrastructure.Integrations.OpenMeteo;

internal sealed class OpenMeteoApiResponse
{
    [JsonPropertyName("current")]
    public OpenMeteoCurrentBlock? Current { get; set; }

    [JsonPropertyName("hourly")]
    public OpenMeteoHourlyBlock? Hourly { get; set; }
}

internal sealed class OpenMeteoCurrentBlock
{
    [JsonPropertyName("time")]
    public string? Time { get; set; }

    [JsonPropertyName("precipitation")]
    public double Precipitation { get; set; }

    [JsonPropertyName("weather_code")]
    public int WeatherCode { get; set; }
}

internal sealed class OpenMeteoHourlyBlock
{
    [JsonPropertyName("time")]
    public string[]? Time { get; set; }

    [JsonPropertyName("precipitation")]
    public double[]? Precipitation { get; set; }

    [JsonPropertyName("precipitation_probability")]
    public int[]? PrecipitationProbability { get; set; }

    [JsonPropertyName("weather_code")]
    public int[]? WeatherCode { get; set; }
}
