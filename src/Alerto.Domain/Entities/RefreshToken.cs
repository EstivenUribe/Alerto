using Alerto.Domain.Common;
using Alerto.Domain.Exceptions;

namespace Alerto.Domain.Entities;

public sealed class RefreshToken : BaseEntity
{
    private RefreshToken()
    {
    }

    private RefreshToken(
        Guid userId,
        string token,
        DateTime expiresAtUtc,
        string createdByIp)
    {
        UserId = userId;
        Token = token;
        ExpiresAtUtc = expiresAtUtc;
        CreatedByIp = createdByIp;
    }

    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public string CreatedByIp { get; private set; } = string.Empty;
    public bool IsActive => RevokedAtUtc is null && ExpiresAtUtc > DateTime.UtcNow;

    public static RefreshToken Issue(Guid userId, string token, DateTime expiresAtUtc, string createdByIp)
    {
        var normalizedToken = token?.Trim() ?? string.Empty;
        var normalizedIp = createdByIp?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedToken))
        {
            throw new EntityValidationException("El token de refresco es obligatorio.");
        }

        if (expiresAtUtc <= DateTime.UtcNow)
        {
            throw new EntityValidationException("La expiracion del refresh token debe ser futura.");
        }

        if (string.IsNullOrWhiteSpace(normalizedIp))
        {
            throw new EntityValidationException("La IP de origen del refresh token es obligatoria.");
        }

        return new(userId, normalizedToken, expiresAtUtc, normalizedIp);
    }

    public void Revoke(DateTime utcNow)
    {
        if (RevokedAtUtc.HasValue)
        {
            throw new DomainRuleException("El refresh token ya fue revocado.");
        }

        RevokedAtUtc = utcNow;
        Touch(utcNow);
    }

    public bool IsActiveAt(DateTime utcNow) => RevokedAtUtc is null && ExpiresAtUtc > utcNow;
}
