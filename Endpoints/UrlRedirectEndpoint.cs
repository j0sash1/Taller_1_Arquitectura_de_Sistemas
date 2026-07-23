using Shortly.Application.Interfaces;
namespace Shortly.Endpoints;

/// <summary>
/// Redirects a shortened URL while supporting HTTP conditional requests.
/// ETag and Last-Modified allow clients to validate cached responses and
/// receive HTTP 304 (Not Modified) instead of downloading a full response
/// when the resource has not changed, reducing bandwidth and server work.
/// </summary>
public static class UrlRedirectEndpoint
{
    public static void MapUrlRedirect(this WebApplication app)
    {
        app.MapGet("/{shortUrl}", async (
            HttpContext context,
            string shortUrl,
            ILinkService linkService) =>
        {
            // Problem Details (RFC 7807) returns a machine-readable JSON
            // error body (title/status/detail) instead of plain text, so
            // API clients can parse every error the same way.
            if (string.IsNullOrWhiteSpace(shortUrl) || shortUrl.Length > 32)
            {
                return Results.Problem(
                    title: "Invalid short code",
                    detail: "The short code must be between 1 and 32 characters.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            try
            {
                var link = await linkService.GetLink(shortUrl);

                // An expired link is gone for good (until someone resets it),
                // so 410 is more accurate than 404 and tells clients to stop
                // retrying/caching it.
                if (link.ExpiresAt is not null && link.ExpiresAt <= DateTime.UtcNow)
                {
                    return Results.Problem(
                        title: "Short link expired",
                        detail: $"The short code '{shortUrl}' expired on {link.ExpiresAt:u}.",
                        statusCode: StatusCodes.Status410Gone);
                }

                // Generates a validator representing the current state of the link.
                var etag = $"\"{link.ShortUrl}-{link.CreatedAt.Ticks}\"";

                context.Response.Headers.ETag = etag;
                context.Response.Headers.CacheControl = "public, max-age=60";
                context.Response.Headers.LastModified =
                    link.UpdatedAt.ToUniversalTime().ToString("R");

                
                var ifNoneMatch = context.Request.Headers.IfNoneMatch.ToString();

                // Return 304 when the client already has the latest representation.
                if (ifNoneMatch == etag)
                {
                    return Results.StatusCode(StatusCodes.Status304NotModified);
                }
                // Return 304 when the resource has not changed since the client's cached copy.
                if (DateTimeOffset.TryParse(
                    context.Request.Headers.IfModifiedSince,
                    out var modifiedSince))
                {
                    var lastModified = new DateTimeOffset(link.UpdatedAt).ToUniversalTime();

                    // Compare using second precision because HTTP dates do not include milliseconds.
                    if (lastModified.ToUnixTimeSeconds() <= modifiedSince.ToUnixTimeSeconds())
                    {
                        return Results.StatusCode(StatusCodes.Status304NotModified);
                    }
                }

                await linkService.IncrementClicks(link.Id);

                // 301/307/308 tell clients/caches how long they can trust
                // this redirect; a fixed 302 forces re-checking every time.
                if (link.ExpiresAt is not null)
                {
                    // Temporary link: 307 keeps the request method and
                    // signals the target may change before it expires.
                    return Results.Redirect(link.Url, permanent: false, preserveMethod: true);
                }

                if (link.Clicks > 100)
                {
                    // Popular, stable link: safe to cache long-term.
                    return Results.Redirect(link.Url, permanent: true);
                }

                // Default: not enough history yet, cache conservatively.
                return Results.Redirect(link.Url);
            }
            catch (KeyNotFoundException)
            {
                return Results.Problem(
                    title: "Short link not found",
                    detail: $"No link found for short code '{shortUrl}'.",
                    statusCode: StatusCodes.Status404NotFound);
            }
        })
        // Only the "ShortlyClient" origin (Program.cs) can call this
        // endpoint cross-origin; the browser blocks everyone else.
        .RequireCors("ShortlyClient");
    }
}