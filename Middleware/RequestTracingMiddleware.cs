using Serilog.Context;

namespace Shortly.Middleware;

/// <summary>
/// Creates or propagates a request identifier for every HTTP request.
/// The identifier is added to the response headers and enriched into
/// the Serilog logging context so all log entries for the same request
/// can be correlated.
/// </summary>
public sealed class RequestTracingMiddleware
{
    private const string HeaderName = "X-Request-Id";
    private readonly RequestDelegate _next;

    public RequestTracingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = context.Request.Headers[HeaderName].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(requestId))
        {
            requestId = context.TraceIdentifier;
        }
        else
        {
            context.TraceIdentifier = requestId;
        }

        context.Response.Headers[HeaderName] = requestId;

        using (LogContext.PushProperty("RequestId", requestId))
        {
            await _next(context);
        }
    }
}