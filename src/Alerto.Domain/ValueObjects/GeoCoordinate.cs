using Alerto.Domain.Common;
using Alerto.Domain.Exceptions;

namespace Alerto.Domain.ValueObjects;

public sealed class GeoCoordinate : ValueObject
{
    private GeoCoordinate(decimal latitude, decimal longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public decimal Latitude { get; }
    public decimal Longitude { get; }

    public static GeoCoordinate Create(decimal latitude, decimal longitude)
    {
        if (latitude is < -90 or > 90)
        {
            throw new EntityValidationException("La latitud debe estar en el rango [-90, 90].");
        }

        if (longitude is < -180 or > 180)
        {
            throw new EntityValidationException("La longitud debe estar en el rango [-180, 180].");
        }

        return new GeoCoordinate(decimal.Round(latitude, 6), decimal.Round(longitude, 6));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
    }
}
