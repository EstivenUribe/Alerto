namespace Alerto.Infrastructure.Authentication;

public sealed class MachineClientOptions
{
    public const string SectionName = "MachineClients";

    public List<MachineClientEntry> Clients { get; init; } = [];
}

public sealed class MachineClientEntry
{
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Role { get; init; } = "RulesEngine";
    public string Scope { get; init; } = "alerts:dispatch alerts:read";
}
