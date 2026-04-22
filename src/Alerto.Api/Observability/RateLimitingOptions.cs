namespace Alerto.Api.Observability;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public bool Enabled { get; set; } = true;
    public int PermitLimit { get; set; } = 120;
    public int WindowSeconds { get; set; } = 60;
    public int QueueLimit { get; set; } = 0;
    public int AuthPermitLimit { get; set; } = 20;
    public int AuthWindowSeconds { get; set; } = 60;
}
