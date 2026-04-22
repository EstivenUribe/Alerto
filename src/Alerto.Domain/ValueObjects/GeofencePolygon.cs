using Alerto.Domain.Common;
using Alerto.Domain.Exceptions;

namespace Alerto.Domain.ValueObjects;

public sealed class GeofencePolygon : ValueObject
{
    private GeofencePolygon(string wkt)
    {
        Wkt = wkt;
    }

    public string Wkt { get; }

    public static GeofencePolygon Create(string wkt)
    {
        var normalized = wkt?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new EntityValidationException("La geometria de la geocerca es obligatoria.");
        }

        if (normalized.Length > 5000)
        {
            throw new EntityValidationException("La geometria WKT de la geocerca no puede superar 5000 caracteres.");
        }

        if (!normalized.StartsWith("POLYGON", StringComparison.OrdinalIgnoreCase) &&
            !normalized.StartsWith("MULTIPOLYGON", StringComparison.OrdinalIgnoreCase))
        {
            throw new EntityValidationException("La geometria de la geocerca debe estar en formato WKT POLYGON o MULTIPOLYGON.");
        }

        return new GeofencePolygon(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Wkt;
    }

    public override string ToString() => Wkt;
}
