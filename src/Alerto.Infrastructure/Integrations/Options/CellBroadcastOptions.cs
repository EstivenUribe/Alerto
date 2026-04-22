namespace Alerto.Infrastructure.Integrations.Options;

public sealed class CellBroadcastOptions
{
    public const string SectionName = "Integrations:CellBroadcast";

    public string BaseUrl { get; set; } = "https://cell-broadcast.local/";
    public bool SimulationMode { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 5;
    public int RetryCount { get; set; } = 2;
    public int CircuitBreakerFailures { get; set; } = 3;
    public int CircuitBreakerBreakSeconds { get; set; } = 45;
    public int DuplicateWindowMinutes { get; set; } = 10;
    public string HealthPath { get; set; } = "/health";
}
