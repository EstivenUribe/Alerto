namespace Alerto.Infrastructure.Integrations.Options;

public sealed class SiataOptions
{
    public const string SectionName = "Integrations:Siata";

    public string BaseUrl { get; set; } = "https://siata.local/";
    public bool SimulationMode { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 3;
    public int RetryCount { get; set; } = 2;
    public int CircuitBreakerFailures { get; set; } = 3;
    public int CircuitBreakerBreakSeconds { get; set; } = 30;
    public int CacheMinutes { get; set; } = 5;
    public string HealthPath { get; set; } = "/health";
}
