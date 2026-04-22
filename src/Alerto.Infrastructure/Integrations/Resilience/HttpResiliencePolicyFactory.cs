using Polly;
using Polly.Timeout;
using Microsoft.Extensions.Logging;

namespace Alerto.Infrastructure.Integrations.Resilience;

internal static class HttpResiliencePolicyFactory
{
    public static IAsyncPolicy<HttpResponseMessage> Create(
        string dependencyName,
        int timeoutSeconds,
        int retryCount,
        int circuitBreakerFailures,
        int circuitBreakerBreakSeconds,
        ILogger logger)
    {
        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(timeoutSeconds));

        var retryPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<TimeoutRejectedException>()
            .OrResult(ShouldRetry)
            .WaitAndRetryAsync(
                retryCount,
                retryAttempt => TimeSpan.FromMilliseconds(200 * retryAttempt),
                (outcome, delay, retryAttempt, _) =>
                {
                    var reason = outcome.Exception?.Message ?? $"HTTP {(int?)outcome.Result?.StatusCode}";
                    logger.LogWarning(
                        "Retry {RetryAttempt} para dependencia {DependencyName} en {Delay}ms. Reason={Reason}",
                        retryAttempt,
                        dependencyName,
                        delay.TotalMilliseconds,
                        reason);
                });

        var circuitBreakerPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<TimeoutRejectedException>()
            .OrResult(ShouldRetry)
            .CircuitBreakerAsync(
                circuitBreakerFailures,
                TimeSpan.FromSeconds(circuitBreakerBreakSeconds),
                (outcome, duration) =>
                {
                    var reason = outcome.Exception?.Message ?? $"HTTP {(int?)outcome.Result?.StatusCode}";
                    logger.LogError(
                        "Circuit opened for {DependencyName} during {DurationSeconds}s. Reason={Reason}",
                        dependencyName,
                        duration.TotalSeconds,
                        reason);
                },
                () => logger.LogInformation("Circuit reset for {DependencyName}", dependencyName));

        return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy, timeoutPolicy);
    }

    private static bool ShouldRetry(HttpResponseMessage response) =>
        (int)response.StatusCode >= 500 || response.StatusCode == System.Net.HttpStatusCode.RequestTimeout;
}
