using Alerto.Domain.Entities;
using Alerto.Domain.Exceptions;

namespace Alerto.DomainTests;

public sealed class GeofenceTests
{
    private static readonly DateTime UtcNow = new DateTime(2026, 4, 21, 10, 0, 0, DateTimeKind.Utc);
    private const string ValidWkt = "POLYGON((-75.575 6.250,-75.565 6.250,-75.565 6.260,-75.575 6.260,-75.575 6.250))";

    // ── Create ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_ShouldBeActiveByDefault()
    {
        var geofence = Geofence.Create("MED-NORTE", "Norte Medellín", ValidWkt, "Aranjuez", UtcNow);

        geofence.IsActive.Should().BeTrue();
        geofence.Code.Should().Be("MED-NORTE");
        geofence.Version.Should().Be(0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyCode_ShouldThrowEntityValidationException(string code)
    {
        var act = () => Geofence.Create(code, "Nombre", ValidWkt, "Barrio", UtcNow);

        act.Should().Throw<EntityValidationException>();
    }

    [Fact]
    public void Create_WithCodeExceeding30Chars_ShouldThrowEntityValidationException()
    {
        var longCode = new string('A', 31);

        var act = () => Geofence.Create(longCode, "Nombre", ValidWkt, "Barrio", UtcNow);

        act.Should().Throw<EntityValidationException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("NOT A POLYGON")]
    [InlineData("LINESTRING(0 0, 1 1)")]
    public void Create_WithInvalidPolygonWkt_ShouldThrowEntityValidationException(string wkt)
    {
        var act = () => Geofence.Create("CODE-1", "Nombre", wkt, "Barrio", UtcNow);

        act.Should().Throw<EntityValidationException>();
    }

    // ── Update ──────────────────────────────────────────────────────────────

    [Fact]
    public void Update_ShouldIncrementVersion()
    {
        var geofence = Geofence.Create("MED-NORTE", "Norte Medellín", ValidWkt, "Aranjuez", UtcNow);

        geofence.Update("Nuevo Nombre", ValidWkt, "Nuevo Barrio", true, UtcNow.AddMinutes(1));

        geofence.Version.Should().Be(1);
        geofence.Name.Should().Be("Nuevo Nombre");
    }

    [Fact]
    public void Update_CanDeactivateGeofence()
    {
        var geofence = Geofence.Create("MED-NORTE", "Norte Medellín", ValidWkt, "Aranjuez", UtcNow);

        geofence.Update("Norte Medellín", ValidWkt, "Aranjuez", false, UtcNow.AddMinutes(1));

        geofence.IsActive.Should().BeFalse();
    }
}
