using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace Alerto.Api.Observability;

public interface IApiMetrics
{
    void RequestStarted(string method, string route);
    void RequestCompleted(string method, string route, int statusCode, double elapsedMilliseconds);
    void RequestFailed(string method, string route, string exceptionType);
    MetricsSnapshot GetSnapshot();
}

public sealed class ApiMetrics : IApiMetrics
{
    private static readonly Meter Meter = new("Alerto.Api", "1.0.0");

    private readonly Counter<long> _requestCounter = Meter.CreateCounter<long>("alerto.api.requests");
    private readonly Counter<long> _exceptionCounter = Meter.CreateCounter<long>("alerto.api.exceptions");
    private readonly UpDownCounter<long> _activeRequestsCounter = Meter.CreateUpDownCounter<long>("alerto.api.requests.active");
    private readonly Histogram<double> _requestDurationHistogram = Meter.CreateHistogram<double>("alerto.api.request.duration.ms");

    private long _activeRequests;
    private long _totalRequests;
    private long _totalFailures;
    private readonly ConcurrentDictionary<int, long> _statusCodes = new();
    private readonly ConcurrentDictionary<string, long> _routes = new(StringComparer.OrdinalIgnoreCase);
    private long _lastRequestDurationBits;

    public void RequestStarted(string method, string route)
    {
        Interlocked.Increment(ref _activeRequests);
        _activeRequestsCounter.Add(1, KeyValuePair.Create<string, object?>("method", method), KeyValuePair.Create<string, object?>("route", route));
    }

    public void RequestCompleted(string method, string route, int statusCode, double elapsedMilliseconds)
    {
        Interlocked.Decrement(ref _activeRequests);
        Interlocked.Increment(ref _totalRequests);
        Interlocked.Exchange(ref _lastRequestDurationBits, BitConverter.DoubleToInt64Bits(elapsedMilliseconds));

        _requestCounter.Add(1,
            KeyValuePair.Create<string, object?>("method", method),
            KeyValuePair.Create<string, object?>("route", route),
            KeyValuePair.Create<string, object?>("status_code", statusCode));

        _requestDurationHistogram.Record(elapsedMilliseconds,
            KeyValuePair.Create<string, object?>("method", method),
            KeyValuePair.Create<string, object?>("route", route),
            KeyValuePair.Create<string, object?>("status_code", statusCode));

        _activeRequestsCounter.Add(-1, KeyValuePair.Create<string, object?>("method", method), KeyValuePair.Create<string, object?>("route", route));
        _statusCodes.AddOrUpdate(statusCode, 1, static (_, current) => current + 1);
        _routes.AddOrUpdate($"{method} {route}", 1, static (_, current) => current + 1);
    }

    public void RequestFailed(string method, string route, string exceptionType)
    {
        Interlocked.Increment(ref _totalFailures);
        _exceptionCounter.Add(1,
            KeyValuePair.Create<string, object?>("method", method),
            KeyValuePair.Create<string, object?>("route", route),
            KeyValuePair.Create<string, object?>("exception_type", exceptionType));
    }

    public MetricsSnapshot GetSnapshot()
    {
        return new MetricsSnapshot(
            DateTime.UtcNow,
            Interlocked.Read(ref _activeRequests),
            Interlocked.Read(ref _totalRequests),
            Interlocked.Read(ref _totalFailures),
            BitConverter.Int64BitsToDouble(Interlocked.Read(ref _lastRequestDurationBits)),
            _statusCodes.OrderBy(entry => entry.Key).ToDictionary(entry => entry.Key.ToString(), entry => entry.Value),
            _routes.OrderByDescending(entry => entry.Value).Take(10).ToDictionary(entry => entry.Key, entry => entry.Value));
    }
}

public sealed record MetricsSnapshot(
    DateTime GeneratedAtUtc,
    long ActiveRequests,
    long TotalRequests,
    long TotalFailures,
    double LastRequestDurationMs,
    IReadOnlyDictionary<string, long> StatusCodes,
    IReadOnlyDictionary<string, long> TopRoutes);
