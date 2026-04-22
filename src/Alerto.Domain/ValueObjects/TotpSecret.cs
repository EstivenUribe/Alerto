using Alerto.Domain.Common;
using Alerto.Domain.Exceptions;

namespace Alerto.Domain.ValueObjects;

public sealed class TotpSecret : ValueObject
{
    private TotpSecret(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static TotpSecret Create(string value)
    {
        var normalized = value?.Trim().Replace(" ", string.Empty, StringComparison.Ordinal) ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new EntityValidationException("La semilla TOTP es obligatoria.");
        }

        if (normalized.Length < 16 || normalized.Length > 128)
        {
            throw new EntityValidationException("La semilla TOTP debe tener entre 16 y 128 caracteres.");
        }

        return new TotpSecret(normalized);
    }

    public static TotpSecret? FromStoredValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : Create(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
