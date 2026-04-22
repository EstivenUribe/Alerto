namespace Alerto.Infrastructure.Integrations.Options;

public sealed class NotificationPublisherOptions
{
    public const string SectionName = "Integrations:NotificationPublisher";

    public bool SimulationMode { get; set; } = true;
    public bool UseOutbox { get; set; } = true;
    public string NotificationTopic { get; set; } = "alerto.notifications";
    public string AuditTopic { get; set; } = "alerto.audit";
}
