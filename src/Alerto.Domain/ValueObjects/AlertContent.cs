using Alerto.Domain.Common;
using Alerto.Domain.Exceptions;

namespace Alerto.Domain.ValueObjects;

public sealed class AlertContent : ValueObject
{
    private AlertContent(string title, string description)
    {
        Title = title;
        Description = description;
    }

    public string Title { get; }
    public string Description { get; }

    public static AlertContent Create(string title, string description)
    {
        var normalizedTitle = title?.Trim() ?? string.Empty;
        var normalizedDescription = description?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedTitle))
        {
            throw new EntityValidationException("El titulo de la alerta es obligatorio.");
        }

        if (normalizedTitle.Length > 160)
        {
            throw new EntityValidationException("El titulo de la alerta no puede superar 160 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(normalizedDescription))
        {
            throw new EntityValidationException("La descripcion de la alerta es obligatoria.");
        }

        if (normalizedDescription.Length > 2000)
        {
            throw new EntityValidationException("La descripcion de la alerta no puede superar 2000 caracteres.");
        }

        return new AlertContent(normalizedTitle, normalizedDescription);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Title;
        yield return Description;
    }
}
