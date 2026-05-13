namespace Alerto.Infrastructure.Integrations.OpenMeteo;

public sealed class OpenMeteoOptions
{
    public const string SectionName = "Integrations:OpenMeteo";

    public string BaseUrl { get; set; } = "https://api.open-meteo.com/";
    public int TimeoutSeconds { get; set; } = 5;
    public int RetryCount { get; set; } = 2;
    public int CircuitBreakerFailures { get; set; } = 3;
    public int CircuitBreakerBreakSeconds { get; set; } = 60;
}
