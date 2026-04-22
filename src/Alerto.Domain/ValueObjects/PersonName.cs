using Alerto.Domain.Common;
using Alerto.Domain.Exceptions;

namespace Alerto.Domain.ValueObjects;

public sealed class PersonName : ValueObject
{
    private PersonName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static PersonName Create(string value)
    {
        var normalized = value?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new EntityValidationException("El nombre visible del usuario es obligatorio.");
        }

        if (normalized.Length > 120)
        {
            throw new EntityValidationException("El nombre visible del usuario no puede superar 120 caracteres.");
        }

        return new PersonName(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
