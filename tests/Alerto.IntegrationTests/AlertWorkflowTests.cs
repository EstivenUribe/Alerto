using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace Alerto.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class AlertWorkflowTests
{
    private readonly AlertoApiFactory _factory;

    public AlertWorkflowTests(AlertoApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_Live_Should_Return_Ok()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        using var client = _factory.CreateClient(new()
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/health/live");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Login_Create_And_Approve_Alert_Should_Work_EndToEnd()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        using var client = _factory.CreateClient(new()
        {
            BaseAddress = new Uri("https://localhost")
        });

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            username = "admin",
            password = "AlertoAdmin123!"
        });

        loginResponse.EnsureSuccessStatusCode();

        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = loginPayload.GetProperty("accessToken").GetString();
        accessToken.Should().NotBeNullOrWhiteSpace();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var geofenceResponse = await client.GetAsync("/api/v1/geofences?activeOnly=true");
        geofenceResponse.EnsureSuccessStatusCode();

        var geofences = await geofenceResponse.Content.ReadFromJsonAsync<JsonElement>();
        var geofenceId = geofences.EnumerateArray().First().GetProperty("id").GetGuid();

        var createResponse = await client.PostAsJsonAsync("/api/v1/alerts", new
        {
            title = "Prueba de sirena académica",
            description = "Simulación de evacuación preventiva por creciente súbita.",
            severity = "Critical",
            sourceSystem = "Tablero COE",
            address = "Carrera 52 #44-31, Medellín",
            latitude = 6.25184m,
            longitude = -75.56359m,
            geofenceId
        });

        createResponse.EnsureSuccessStatusCode();
        var createPayload = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var alertId = createPayload.GetProperty("id").GetGuid();
        var version = createPayload.GetProperty("version").GetInt32();

        var approveResponse = await client.PostAsJsonAsync($"/api/v1/alerts/{alertId}/approve", new
        {
            expectedVersion = version
        });

        approveResponse.EnsureSuccessStatusCode();
        var approvedPayload = await approveResponse.Content.ReadFromJsonAsync<JsonElement>();

        approvedPayload.GetProperty("status").GetString().Should().Be("Approved");
    }
}
