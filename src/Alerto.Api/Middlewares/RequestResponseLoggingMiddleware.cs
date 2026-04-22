using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Alerto.Api.Observability;
using Microsoft.Extensions.Options;

namespace Alerto.Api.Middlewares;

public sealed class RequestResponseLoggingMiddleware
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
    private readonly ObservabilityOptions _options;
    private readonly IApiMetrics _metrics;

    public RequestResponseLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestResponseLoggingMiddleware> logger,
        IOptions<ObservabilityOptions> options,
        IApiMetrics metrics)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
        _metrics = metrics;
    }

    public async Task Invoke(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var route = GetRoute(context);
        var method = context.Request.Method;

        _metrics.RequestStarted(method, route);

        var requestPayload = await CaptureRequestAsync(context);
        var originalResponseBody = context.Response.Body;
        await using var responseBuffer = new MemoryStream();
        context.Response.Body = responseBuffer;

        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _metrics.RequestFailed(method, route, exception.GetType().Name);
            throw;
        }
        finally
        {
            stopwatch.Stop();
        }

        var responsePayload = await CaptureResponseAsync(context, responseBuffer, originalResponseBody);
        var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
        _metrics.RequestCompleted(method, route, context.Response.StatusCode, elapsedMs);

        _logger.LogInformation(
            "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms. CorrelationId={CorrelationId} Request={RequestPayload} Response={ResponsePayload}",
            method,
            context.Request.Path,
            context.Response.StatusCode,
            Math.Round(elapsedMs, 2),
            context.TraceIdentifier,
            requestPayload,
            responsePayload);
    }

    private async Task<string> CaptureRequestAsync(HttpContext context)
    {
        if (!_options.EnableBodyLogging || !CanCaptureBody(context.Request.ContentType) || context.Request.ContentLength is > 1024 * 256)
        {
            return BuildMetadataOnlyRequest(context);
        }

        context.Request.EnableBuffering();
        context.Request.Body.Position = 0;

        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        return BuildRequestLog(context, body);
    }

    private async Task<string> CaptureResponseAsync(HttpContext context, MemoryStream responseBuffer, Stream originalResponseBody)
    {
        responseBuffer.Position = 0;
        var responseText = await new StreamReader(responseBuffer, Encoding.UTF8, leaveOpen: true).ReadToEndAsync();
        responseBuffer.Position = 0;
        await responseBuffer.CopyToAsync(originalResponseBody);
        context.Response.Body = originalResponseBody;

        if (!_options.EnableBodyLogging || !CanCaptureBody(context.Response.ContentType) || responseText.Length > _options.MaxLoggedBodyLength)
        {
            return JsonSerializer.Serialize(new
            {
                statusCode = context.Response.StatusCode,
                contentType = context.Response.ContentType,
                contentLength = context.Response.ContentLength
            }, SerializerOptions);
        }

        return SanitizeJsonPayload(responseText);
    }

    private string BuildRequestLog(HttpContext context, string body)
    {
        var payload = new
        {
            method = context.Request.Method,
            path = context.Request.Path.Value,
            query = context.Request.QueryString.Value,
            contentType = context.Request.ContentType,
            contentLength = context.Request.ContentLength,
            body = body.Length > _options.MaxLoggedBodyLength
                ? "[omitted: body too large]"
                : SanitizeJsonPayload(body)
        };

        return JsonSerializer.Serialize(payload, SerializerOptions);
    }

    private static string BuildMetadataOnlyRequest(HttpContext context)
    {
        return JsonSerializer.Serialize(new
        {
            method = context.Request.Method,
            path = context.Request.Path.Value,
            query = context.Request.QueryString.Value,
            contentType = context.Request.ContentType,
            contentLength = context.Request.ContentLength
        }, SerializerOptions);
    }

    private string SanitizeJsonPayload(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return "{}";
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            var sanitized = SanitizeElement(document.RootElement);
            return JsonSerializer.Serialize(sanitized, SerializerOptions);
        }
        catch (JsonException)
        {
            return payload.Length > _options.MaxLoggedBodyLength
                ? payload[.._options.MaxLoggedBodyLength]
                : payload;
        }
    }

    private object? SanitizeElement(JsonElement element, string? propertyName = null)
    {
        if (!string.IsNullOrWhiteSpace(propertyName) &&
            _options.SensitiveFields.Any(field => string.Equals(field, propertyName, StringComparison.OrdinalIgnoreCase)))
        {
            return "[REDACTED]";
        }

        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(
                property => property.Name,
                property => SanitizeElement(property.Value, property.Name)),
            JsonValueKind.Array => element.EnumerateArray().Select(item => SanitizeElement(item)).ToArray(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var longValue)
                ? longValue
                : element.TryGetDouble(out var doubleValue)
                    ? doubleValue
                    : element.GetRawText(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.GetRawText()
        };
    }

    private static bool CanCaptureBody(string? contentType) =>
        !string.IsNullOrWhiteSpace(contentType) &&
        contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase);

    private static string GetRoute(HttpContext context) =>
        context.GetEndpoint()?.DisplayName ?? context.Request.Path.Value ?? "unknown";
}
