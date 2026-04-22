using Alerto.Domain.Common;
using Alerto.Domain.Exceptions;
using Alerto.Domain.ValueObjects;

namespace Alerto.Domain.Entities;

public sealed class Geofence : BaseEntity
{
    private Geofence()
    {
    }

    private Geofence(
        string code,
        string name,
        string polygonWkt,
        string neighborhood,
        bool isActive,
        DateTime utcNow)
    {
        Code = code;
        Name = name;
        PolygonWkt = polygonWkt;
        Neighborhood = neighborhood;
        IsActive = isActive;
        CreatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string PolygonWkt { get; private set; } = string.Empty;
    public string Neighborhood { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public GeofencePolygon Polygon => GeofencePolygon.Create(PolygonWkt);

    public static Geofence Create(
        string code,
        string name,
        string polygonWkt,
        string neighborhood,
        DateTime utcNow)
        => new(
            Normalize(code, nameof(code), 30),
            Normalize(name, nameof(name), 120),
            GeofencePolygon.Create(polygonWkt).Wkt,
            Normalize(neighborhood, nameof(neighborhood), 120),
            true,
            utcNow);

    public void Update(string name, string polygonWkt, string neighborhood, bool isActive, DateTime utcNow)
    {
        Name = Normalize(name, nameof(name), 120);
        PolygonWkt = GeofencePolygon.Create(polygonWkt).Wkt;
        Neighborhood = Normalize(neighborhood, nameof(neighborhood), 120);
        IsActive = isActive;
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
