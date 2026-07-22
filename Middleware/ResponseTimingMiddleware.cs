using System.Diagnostics;

namespace Shortly.Middleware;

/// <summary>
/// Measures request execution time, adds the X-Response-Time header,
/// and logs requests that take more than 500 ms.
/// </summary>
public class ResponseTimingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ResponseTimingMiddleware> _logger;

    public ResponseTimingMiddleware(
        RequestDelegate next,
        ILogger<ResponseTimingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        var sw = Stopwatch.StartNew();

        await _next(context);

        sw.Stop();

        context.Response.Headers["X-Response-Time"] =
            $"{sw.ElapsedMilliseconds}ms";

        if (sw.ElapsedMilliseconds > 500)
        {
            _logger.LogWarning(
                "Slow Request {Method} {Path} {StatusCode} {Elapsed}ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                sw.ElapsedMilliseconds);
        }
    }
}