namespace Alerto.Application.Alerts;

public sealed class AlertOptions
{
    public const string SectionName = "Alerts";

    public int ApprovalTimeoutMinutes { get; init; } = 3;
    public int CacheTtlMinutes { get; init; } = 5;
}
