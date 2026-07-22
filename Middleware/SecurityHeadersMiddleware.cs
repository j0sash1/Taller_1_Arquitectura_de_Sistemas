namespace Shortly.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        context.Response.Headers["Strict-Transport-Security"] =
            "max-age=31536000; includeSubDomains";

        context.Response.Headers["X-Content-Type-Options"] =
            "nosniff";

        context.Response.Headers["X-Frame-Options"] =
            "DENY";

        context.Response.Headers["Referrer-Policy"] =
            "strict-origin-when-cross-origin";

        context.Response.Headers["Permissions-Policy"] =
            "geolocation=(), camera=(), microphone=()";

        await _next(context);
    }
}