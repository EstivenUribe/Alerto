using System.Text.RegularExpressions;
using Alerto.Domain.Common;
using Alerto.Domain.Exceptions;

namespace Alerto.Domain.ValueObjects;

public sealed class EmailAddress : ValueObject
{
    private static readonly Regex EmailRegex = new(
        "^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private EmailAddress(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static EmailAddress Create(string value)
    {
        var normalized = value?.Trim().ToLowerInvariant() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new EntityValidationException("El correo electronico es obligatorio.");
        }

        if (normalized.Length > 160)
        {
            throw new EntityValidationException("El correo electronico no puede superar 160 caracteres.");
        }

        if (!EmailRegex.IsMatch(normalized))
        {
            throw new EntityValidationException("El correo electronico no tiene un formato valido.");
        }

        return new EmailAddress(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
