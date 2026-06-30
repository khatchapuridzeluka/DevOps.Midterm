namespace DevOps.WebAPI.Observability;

/// <summary>
/// Increments the custom app_requests_total / app_errors_total counters for every
/// request. Errors are counted when the response status code is >= 500 or an
/// unhandled exception bubbles up through the pipeline.
/// </summary>
public class MetricsMiddleware
{
    private readonly RequestDelegate _next;

    public MetricsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Use the routed endpoint name when available, otherwise the raw path.
        var endpoint = context.Request.Path.HasValue ? context.Request.Path.Value! : "unknown";
        var method = context.Request.Method;

        try
        {
            await _next(context);

            var statusCode = context.Response.StatusCode;
            AppMetrics.RequestsTotal.WithLabels(method, endpoint, statusCode.ToString()).Inc();

            if (statusCode >= 500)
            {
                AppMetrics.ErrorsTotal.WithLabels(method, endpoint, statusCode.ToString()).Inc();
            }
        }
        catch
        {
            // Unhandled exception -> count as a request and a 500-class error.
            AppMetrics.RequestsTotal.WithLabels(method, endpoint, "500").Inc();
            AppMetrics.ErrorsTotal.WithLabels(method, endpoint, "500").Inc();
            throw;
        }
    }
}
