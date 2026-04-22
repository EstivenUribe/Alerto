namespace Alerto.Infrastructure.Integrations.Options;

public sealed class OutboxOptions
{
    public const string SectionName = "Integrations:Outbox";

    public bool Enabled { get; set; } = true;
    public bool ProcessInProcess { get; set; } = true;
    public int BatchSize { get; set; } = 20;
    public int PollIntervalSeconds { get; set; } = 10;
}
