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

namespace Alerto.Infrastructure.Integrations.Cap;

public sealed class CapGeneratorClient : ICapGeneratorClient
{
    private readonly HttpClient _httpClient;
    private readonly CapGeneratorOptions _options;
    private readonly IAsyncPolicy<HttpResponseMessage> _policy;

    public CapGeneratorClient(
        HttpClient httpClient,
        IOptions<CapGeneratorOptions> options,
        ILogger<CapGeneratorClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _policy = HttpResiliencePolicyFactory.Create(
            "CAP Generator",
            _options.TimeoutSeconds,
            _options.RetryCount,
            _options.CircuitBreakerFailures,
            _options.CircuitBreakerBreakSeconds,
            logger);
    }

    public async Task<CapAlertDocumentResponse> GenerateAsync(CapAlertDocumentRequest request, CancellationToken cancellationToken)
    {
        if (_options.SimulationMode)
        {
            return BuildSimulatedDocument(request);
        }

        using var message = new HttpRequestMessage(HttpMethod.Post, "api/v1/cap/documents")
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
                throw new ExternalDependencyException("CAP Generator", $"Respuesta HTTP {(int)response.StatusCode}.");
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/xml";

            return new CapAlertDocumentResponse(
                $"cap-{request.AlertId:N}",
                contentType,
                content,
                DateTime.UtcNow,
                false);
        }
        catch (ExternalDependencyException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new ExternalDependencyException("CAP Generator", "No fue posible generar el documento CAP.", exception);
        }
    }

    private static CapAlertDocumentResponse BuildSimulatedDocument(CapAlertDocumentRequest request)
    {
        var documentId = $"cap-{request.AlertId:N}";
        var content = $"""
            <alert>
              <identifier>{documentId}</identifier>
              <sender>{request.Sender}</sender>
              <sent>{DateTime.UtcNow:O}</sent>
              <status>Actual</status>
              <msgType>Alert</msgType>
              <scope>Public</scope>
              <info>
                <language>es-CO</language>
                <event>{request.Title}</event>
                <severity>{request.Severity}</severity>
                <headline>{request.Title}</headline>
                <description>{request.Description}</description>
                <area>
                  <areaDesc>{request.AreaDescription}</areaDesc>
                </area>
              </info>
            </alert>
            """;

        return new CapAlertDocumentResponse(documentId, "application/xml", content, DateTime.UtcNow, true);
    }
}
