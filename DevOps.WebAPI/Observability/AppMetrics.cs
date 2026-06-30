using Prometheus;

namespace DevOps.WebAPI.Observability;

/// <summary>
/// Custom application-level Prometheus metrics exposed on the /metrics endpoint.
/// These complement the built-in HTTP metrics provided by UseHttpMetrics().
/// </summary>
public static class AppMetrics
{
    public static readonly Counter RequestsTotal = Metrics.CreateCounter(
        "app_requests_total",
        "Total number of HTTP requests handled by the application.",
        new CounterConfiguration
        {
            LabelNames = new[] { "method", "endpoint", "status_code" }
        });

    public static readonly Counter ErrorsTotal = Metrics.CreateCounter(
        "app_errors_total",
        "Total number of failed HTTP requests (status code >= 500 or unhandled exceptions).",
        new CounterConfiguration
        {
            LabelNames = new[] { "method", "endpoint", "status_code" }
        });
}
