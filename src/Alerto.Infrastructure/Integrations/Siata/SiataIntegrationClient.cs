using System.Text.Json;
using Alerto.Application.Common.Exceptions;
using Alerto.Application.Common.Interfaces;
using Alerto.Application.Common.Models;
using Alerto.Infrastructure.Integrations.Options;
using Alerto.Infrastructure.Integrations.Resilience;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Polly;

namespace Alerto.Infrastructure.Integrations.Siata;

public sealed class SiataIntegrationClient : ISiataIntegrationClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly IAppCache _cache;
    private readonly SiataOptions _options;
    private readonly IAsyncPolicy<HttpResponseMessage> _policy;

    public SiataIntegrationClient(
        HttpClient httpClient,
        IAppCache cache,
        IOptions<SiataOptions> options,
        ILogger<SiataIntegrationClient> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _options = options.Value;
        _policy = HttpResiliencePolicyFactory.Create(
            "SIATA",
            _options.TimeoutSeconds,
            _options.RetryCount,
            _options.CircuitBreakerFailures,
            _options.CircuitBreakerBreakSeconds,
            logger);
    }

    public async Task<SiataHazardSnapshot> GetHazardSnapshotAsync(SiataHazardQuery query, CancellationToken cancellationToken)
    {
        var cacheKey = $"siata:snapshot:{query.Latitude}:{query.Longitude}:{query.GeofenceId}:{query.Neighborhood}";
        var cached = await _cache.GetAsync<SiataHazardSnapshot>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var snapshot = _options.SimulationMode
            ? BuildSimulatedResponse(query)
            : await GetRemoteSnapshotAsync(query, cancellationToken);

        await _cache.SetAsync(cacheKey, snapshot, TimeSpan.FromMinutes(_options.CacheMinutes), cancellationToken);
        return snapshot;
    }

    private async Task<SiataHazardSnapshot> GetRemoteSnapshotAsync(SiataHazardQuery query, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/v1/hazards/latest?latitude={query.Latitude}&longitude={query.Longitude}&geofenceId={query.GeofenceId}&neighborhood={Uri.EscapeDataString(query.Neighborhood ?? string.Empty)}");

        try
        {
            var response = await _policy.ExecuteAsync(
                token => _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new ExternalDependencyException("SIATA", $"Respuesta HTTP {(int)response.StatusCode}.");
            }

            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseSnapshot(payload);
        }
        catch (ExternalDependencyException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new ExternalDependencyException("SIATA", "No fue posible consultar el estado de amenaza.", exception);
        }
    }

    private static SiataHazardSnapshot ParseSnapshot(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;

        return new SiataHazardSnapshot(
            root.TryGetProperty("source", out var source) ? source.GetString() ?? "SIATA" : "SIATA",
            root.TryGetProperty("summary", out var summary) ? summary.GetString() ?? "Sin resumen" : "Sin resumen",
            root.TryGetProperty("riskLevel", out var riskLevel) ? riskLevel.GetString() ?? "Unknown" : "Unknown",
            root.TryGetProperty("observedAtUtc", out var observedAtUtc) && observedAtUtc.TryGetDateTime(out var value)
                ? value
                : DateTime.UtcNow,
            payload,
            false);
    }

    private static SiataHazardSnapshot BuildSimulatedResponse(SiataHazardQuery query)
    {
        var payload = JsonSerializer.Serialize(new
        {
            source = "SIATA-Simulated",
            summary = $"Condiciones hidrometeorologicas monitoreadas para {query.Neighborhood ?? "Medellin"}.",
            riskLevel = "Moderate",
            observedAtUtc = DateTime.UtcNow
        }, SerializerOptions);

        return new SiataHazardSnapshot(
            "SIATA-Simulated",
            $"Condiciones hidrometeorologicas monitoreadas para {query.Neighborhood ?? "Medellin"}.",
            "Moderate",
            DateTime.UtcNow,
            payload,
            true);
    }
}
