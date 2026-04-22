namespace Alerto.Infrastructure.Authentication;

public sealed class BootstrapAdminOptions
{
    public const string SectionName = "BootstrapAdmin";

    public string Username { get; init; } = "admin";
    public string DisplayName { get; init; } = "Administrador Alerto";
    public string Email { get; init; } = "admin@alerto.local";
    public string Password { get; init; } = "AlertoAdmin123!";
}
