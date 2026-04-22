using Alerto.Domain.Common;
using Alerto.Domain.Enums;
using Alerto.Domain.Events;
using Alerto.Domain.Exceptions;
using Alerto.Domain.ValueObjects;

namespace Alerto.Domain.Entities;

public sealed class User : BaseEntity
{
    private readonly List<RefreshToken> _refreshTokens = [];

    private User()
    {
    }

    private User(
        string username,
        string displayName,
        string email,
        UserRole role,
        string passwordHash,
        DateTime utcNow)
    {
        Username = username;
        DisplayName = displayName;
        Email = email;
        Role = role;
        PasswordHash = passwordHash;
        IsActive = true;
        CreatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public string Username { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public bool IsTwoFactorEnabled { get; private set; }
    public string? TotpSecret { get; private set; }
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();
    public PersonName Name => PersonName.Create(DisplayName);
    public EmailAddress EmailAddress => EmailAddress.Create(Email);
    public TotpSecret? TwoFactorSecret => ValueObjects.TotpSecret.FromStoredValue(TotpSecret);

    public static User Create(
        string username,
        string displayName,
        string email,
        UserRole role,
        string passwordHash,
        DateTime utcNow)
    {
        var user = new User(
            NormalizeUsername(username),
            PersonName.Create(displayName).Value,
            EmailAddress.Create(email).Value,
            role,
            NormalizePasswordHash(passwordHash),
            utcNow);

        user.RaiseDomainEvent(new UserCreatedDomainEvent(user.Id, user.Username, utcNow));
        return user;
    }

    public void UpdateProfile(string displayName, string email, UserRole role, bool isActive, DateTime utcNow)
    {
        DisplayName = PersonName.Create(displayName).Value;
        Email = EmailAddress.Create(email).Value;
        Role = role;
        IsActive = isActive;
        Touch(utcNow);
    }

    public void UpdatePasswordHash(string passwordHash, DateTime utcNow)
    {
        PasswordHash = NormalizePasswordHash(passwordHash);
        Touch(utcNow);
    }

    public void ProvisionTwoFactor(string secret, DateTime utcNow)
    {
        TotpSecret = ValueObjects.TotpSecret.Create(secret).Value;
        IsTwoFactorEnabled = false;
        Touch(utcNow);
    }

    public void EnableTwoFactor(DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(TotpSecret))
        {
            throw new TwoFactorProvisioningException("No existe una semilla TOTP provisionada para este usuario.");
        }

        IsTwoFactorEnabled = true;
        Touch(utcNow);
        RaiseDomainEvent(new TwoFactorEnabledDomainEvent(Id, utcNow));
    }

    public void DisableTwoFactor(DateTime utcNow)
    {
        IsTwoFactorEnabled = false;
        TotpSecret = null;
        Touch(utcNow);
    }

    public void AddRefreshToken(RefreshToken refreshToken, DateTime utcNow)
    {
        _refreshTokens.Add(refreshToken);
        Touch(utcNow);
    }

    public RefreshToken GetActiveRefreshToken(string token)
    {
        var refreshToken = _refreshTokens.SingleOrDefault(x => x.Token == token);
        if (refreshToken is null || !refreshToken.IsActive)
        {
            throw new DomainRuleException("El refresh token no es valido o ya expiro.");
        }

        return refreshToken;
    }

    private static string NormalizeUsername(string username)
    {
        var normalized = username?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new EntityValidationException("El username es obligatorio.");
        }

        if (normalized.Length > 80)
        {
            throw new EntityValidationException("El username no puede superar 80 caracteres.");
        }

        return normalized;
    }

    private static string NormalizePasswordHash(string passwordHash)
    {
        var normalized = passwordHash?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new EntityValidationException("El hash de contraseña es obligatorio.");
        }

        if (normalized.Length > 500)
        {
            throw new EntityValidationException("El hash de contraseña no puede superar 500 caracteres.");
        }

        return normalized;
    }
}
