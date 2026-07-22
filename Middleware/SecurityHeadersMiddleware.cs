namespace Shortly.Middleware;

/// <summary>
/// Adds common security headers to every HTTP response.
/// These headers help protect against clickjacking, MIME sniffing,
/// information leakage and enforce HTTPS usage.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        // Forces browsers to use HTTPS for future requests.
        context.Response.Headers["Strict-Transport-Security"] =
            "max-age=31536000; includeSubDomains";

        // Prevents MIME type sniffing.
        context.Response.Headers["X-Content-Type-Options"] =
            "nosniff";

        // Prevents the application from being embedded in iframes (clickjacking).
        context.Response.Headers["X-Frame-Options"] =
            "DENY";

        // Limits the amount of referrer information sent to other sites.
        context.Response.Headers["Referrer-Policy"] =
            "strict-origin-when-cross-origin";

        // Disables access to sensitive browser features.
        context.Response.Headers["Permissions-Policy"] =
            "geolocation=(), camera=(), microphone=()";

        await _next(context);
    }
}