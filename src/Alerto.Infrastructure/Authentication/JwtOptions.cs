namespace Alerto.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 15;
    public int TwoFactorTokenMinutes { get; init; } = 5;
    public int MachineTokenMinutes { get; init; } = 10;
}
