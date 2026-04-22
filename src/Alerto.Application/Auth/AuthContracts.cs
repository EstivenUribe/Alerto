namespace Alerto.Application.Auth;

public sealed record LoginRequest(string Username, string Password);

public sealed record VerifyTwoFactorRequest(string TwoFactorToken, string Code);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);

public sealed record M2MTokenRequest(string ClientId, string ClientSecret);

public sealed record EnableTwoFactorRequest(string Code);

public sealed record AuthenticationResponse(
    string TokenType,
    string Username,
    string Role,
    bool RequiresTwoFactor,
    string? AccessToken,
    DateTime? AccessTokenExpiresAtUtc,
    string? RefreshToken,
    DateTime? RefreshTokenExpiresAtUtc,
    string? TwoFactorToken,
    DateTime? TwoFactorTokenExpiresAtUtc);

public sealed record TwoFactorSetupResponse(string Secret, string ProvisioningUri, bool IsEnabled);
