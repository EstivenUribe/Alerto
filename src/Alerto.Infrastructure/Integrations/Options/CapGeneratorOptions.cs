namespace Alerto.Infrastructure.Integrations.Options;

public sealed class CapGeneratorOptions
{
    public const string SectionName = "Integrations:CapGenerator";

    public string BaseUrl { get; set; } = "https://cap-generator.local/";
    public bool SimulationMode { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 5;
    public int RetryCount { get; set; } = 2;
    public int CircuitBreakerFailures { get; set; } = 3;
    public int CircuitBreakerBreakSeconds { get; set; } = 30;
    public string HealthPath { get; set; } = "/health";
}
