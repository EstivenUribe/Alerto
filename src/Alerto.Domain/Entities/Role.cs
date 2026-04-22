using Alerto.Domain.Common;
using Alerto.Domain.Enums;
using Alerto.Domain.Exceptions;

namespace Alerto.Domain.Entities;

public sealed class Role : BaseEntity
{
    private Role()
    {
    }

    private Role(UserRole code, string name, string description, bool isSystemRole, DateTime utcNow)
    {
        Code = code;
        Name = Normalize(name, nameof(name), 120);
        Description = Normalize(description, nameof(description), 240);
        IsSystemRole = isSystemRole;
        CreatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public UserRole Code { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsSystemRole { get; private set; }

    public static Role Create(UserRole code, string name, string description, DateTime utcNow)
        => new(code, name, description, true, utcNow);

    public void UpdateDescription(string description, DateTime utcNow)
    {
        Description = Normalize(description, nameof(description), 240);
        Touch(utcNow);
    }

    private static string Normalize(string value, string fieldName, int maxLength)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new EntityValidationException($"El campo '{fieldName}' es obligatorio.");
        }

        if (normalized.Length > maxLength)
        {
            throw new EntityValidationException($"El campo '{fieldName}' no puede superar {maxLength} caracteres.");
        }

        return normalized;
    }
}
