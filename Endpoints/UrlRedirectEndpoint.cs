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
            try
            {
                var link = await linkService.GetLink(shortUrl);

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

                return Results.Redirect(link.Url);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        });
    }
}