using DevOps.WebAPI.Observability;
using DevOps.WebAPI.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Prometheus;
using Serilog;
using Serilog.Formatting.Compact;

// Bootstrap logger so failures during startup are still captured as JSON.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Structured JSON logging to console (for Docker/Loki/ELK) and a rolling file.
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("application", "devops-webapi")
        .WriteTo.Console(new CompactJsonFormatter())
        .WriteTo.File(
            new CompactJsonFormatter(),
            "logs/app-.json",
            rollingInterval: RollingInterval.Day));

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddScoped<ICalculatorService, CalculatorService>();
    builder.Services.AddScoped<IPersonService, PersonService>();

    var app = builder.Build();

    // Emit a structured JSON log line per HTTP request.
    app.UseSerilogRequestLogging();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Calculator API v1");
        c.RoutePrefix = string.Empty; // Swagger at root "/"
    });

    app.UseHttpsRedirection();

    // Record built-in HTTP metrics (request duration, in-flight, etc.).
    app.UseHttpMetrics();

    // Record custom app_requests_total / app_errors_total counters.
    app.UseMiddleware<MetricsMiddleware>();

    app.UseAuthorization();
    app.MapControllers();

    // Expose the Prometheus scrape endpoint at /metrics.
    app.MapMetrics();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}