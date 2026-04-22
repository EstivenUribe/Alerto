using System.Text;
using System.Text.Json;
using Alerto.Application.Common.Exceptions;
using Alerto.Application.Common.Interfaces;
using Alerto.Application.Common.Models;
using Alerto.Infrastructure.Integrations.Options;
using Alerto.Infrastructure.Integrations.Resilience;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Polly;

namespace Alerto.Infrastructure.Integrations.Dispatch;

public sealed class CellBroadcastDispatcher : ICellBroadcastDispatcher
{
    private readonly HttpClient _httpClient;
    private readonly IDispatchIdempotencyStore _idempotencyStore;
    private readonly CellBroadcastOptions _options;
    private readonly IAsyncPolicy<HttpResponseMessage> _policy;

    public CellBroadcastDispatcher(
        HttpClient httpClient,
        IDispatchIdempotencyStore idempotencyStore,
        IOptions<CellBroadcastOptions> options,
        ILogger<CellBroadcastDispatcher> logger)
    {
        _httpClient = httpClient;
        _idempotencyStore = idempotencyStore;
        _options = options.Value;
        _policy = HttpResiliencePolicyFactory.Create(
            "Cell Broadcast",
            _options.TimeoutSeconds,
            _options.RetryCount,
            _options.CircuitBreakerFailures,
            _options.CircuitBreakerBreakSeconds,
            logger);
    }

    public async Task<CellBroadcastDispatchResponse> DispatchAsync(
        CellBroadcastDispatchRequest request,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = $"{request.AlertId:N}:{request.AreaCode}:{request.ProviderReference}";
        var ttl = TimeSpan.FromMinutes(_options.DuplicateWindowMinutes);
        var acquired = await _idempotencyStore.TryAcquireAsync(idempotencyKey, ttl, cancellationToken);
        if (!acquired)
        {
            return new CellBroadcastDispatchResponse(
                request.ProviderReference,
                "Suppressed",
                true,
                _options.SimulationMode,
                DateTime.UtcNow);
        }

        try
        {
            return _options.SimulationMode
                ? BuildSimulatedDispatchResponse(request)
                : await SendRemoteDispatchAsync(request, cancellationToken);
        }
        catch
        {
            await _idempotencyStore.ReleaseAsync(idempotencyKey, cancellationToken);
            throw;
        }
    }

    private async Task<CellBroadcastDispatchResponse> SendRemoteDispatchAsync(
        CellBroadcastDispatchRequest request,
        CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "api/v1/broadcasts/cell")
        {
            Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
        };

        try
        {
            var response = await _policy.ExecuteAsync(
                token => _httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, token),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new ExternalDependencyException("Cell Broadcast", $"Respuesta HTTP {(int)response.StatusCode}.");
            }

            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            return new CellBroadcastDispatchResponse(
                root.TryGetProperty("providerMessageId", out var providerMessageId)
                    ? providerMessageId.GetString() ?? request.ProviderReference
                    : request.ProviderReference,
                root.TryGetProperty("status", out var status) ? status.GetString() ?? "Accepted" : "Accepted",
                false,
                false,
                DateTime.UtcNow);
        }
        catch (ExternalDependencyException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new ExternalDependencyException("Cell Broadcast", "No fue posible entregar el mensaje al difusor.", exception);
        }
    }

    private static CellBroadcastDispatchResponse BuildSimulatedDispatchResponse(CellBroadcastDispatchRequest request)
    {
        return new CellBroadcastDispatchResponse(
            $"cb-{request.AlertId:N}",
            "Accepted",
            false,
            true,
            DateTime.UtcNow);
    }
}
