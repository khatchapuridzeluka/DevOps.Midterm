using Microsoft.AspNetCore.Mvc;

namespace DevOps.WebAPI.Controllers;

/// <summary>
/// Endpoints used to deliberately generate failures so the
/// app_errors_total counter (and the CRITICAL Prometheus alert) can be demonstrated.
/// </summary>
[ApiController]
public class SimulateController : ControllerBase
{
    private readonly ILogger<SimulateController> _logger;

    public SimulateController(ILogger<SimulateController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Returns HTTP 500. Each call increments app_errors_total by one
    /// (the MetricsMiddleware counts any response with status code >= 500 as an error).
    /// </summary>
    [HttpGet("/simulate-error")]
    public IActionResult SimulateError()
    {
        _logger.LogError("Simulated error triggered via /simulate-error at {Timestamp}", DateTimeOffset.UtcNow);
        return Problem(
            detail: "This is a simulated error used to drive app_errors_total.",
            statusCode: StatusCodes.Status500InternalServerError,
            title: "Simulated Error");
    }

    /// <summary>
    /// Fires a burst of simulated errors in a single call so the error rate quickly
    /// exceeds the 5-per-minute alert threshold. Example: /simulate-error/burst?count=10
    /// </summary>
    [HttpGet("/simulate-error/burst")]
    public IActionResult SimulateErrorBurst([FromQuery] int count = 10)
    {
        count = Math.Clamp(count, 1, 100);

        for (var i = 0; i < count; i++)
        {
            // Counts as an explicit error in app_errors_total via the metrics middleware.
            Observability.AppMetrics.ErrorsTotal
                .WithLabels("GET", "/simulate-error/burst", "500")
                .Inc();

            _logger.LogError(
                "Simulated burst error {Index}/{Count} via /simulate-error/burst",
                i + 1, count);
        }

        return Problem(
            detail: $"Generated {count} simulated errors to exceed the alert threshold.",
            statusCode: StatusCodes.Status500InternalServerError,
            title: "Simulated Error Burst");
    }
}
