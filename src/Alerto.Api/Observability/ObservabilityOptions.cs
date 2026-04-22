namespace Alerto.Api.Observability;

public sealed class ObservabilityOptions
{
    public const string SectionName = "Observability";

    public bool EnableBodyLogging { get; set; } = false;
    public int MaxLoggedBodyLength { get; set; } = 2048;
    public string MetricsRoute { get; set; } = "/metrics/basic";
    public string[] SensitiveFields { get; set; } =
    [
        "password",
        "newPassword",
        "clientSecret",
        "refreshToken",
        "accessToken",
        "twoFactorToken",
        "code",
        "secret",
        "totpSecret",
        "token"
    ];
}
