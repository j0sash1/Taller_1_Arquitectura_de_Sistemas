namespace Shortly.Endpoints;

/// <summary>
/// Lightweight liveness endpoint for infrastructure probes (load balancers,
/// container orchestrators). Returns JSON with the current status and how
/// long the app has been running, so monitoring tools can parse it without
/// scraping HTML or logs.
/// </summary>
public static class HealthCheckEndpoint
{
    private static readonly DateTime StartedAt = DateTime.UtcNow;

    public static void MapHealthCheck(this WebApplication app)
    {
        app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";

                var payload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    status = report.Status.ToString(),
                    uptime = (DateTime.UtcNow - StartedAt).ToString(@"dd\.hh\:mm\:ss")
                });

                await context.Response.WriteAsync(payload);
            }
        });
    }
}