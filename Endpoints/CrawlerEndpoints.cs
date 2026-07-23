namespace Shortly.Endpoints;

/// <summary>
/// Explicit crawler-control endpoints. Shortly's short codes redirect to
/// arbitrary external URLs, so letting search engines index and follow
/// them serves no purpose and could even help spread spam/phishing links
/// found in indexes. robots.txt disallows crawling entirely; sitemap.xml
/// is kept minimal since there is nothing worth listing for indexing.
/// </summary>
public static class CrawlerEndpoints
{
    public static void MapCrawlerEndpoints(this WebApplication app)
    {
        app.MapGet("/robots.txt", () =>
            Results.Text("User-agent: *\nDisallow: /\n", "text/plain"));

        app.MapGet("/sitemap.xml", () =>
            Results.Text(
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                "<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\"></urlset>",
                "application/xml"));
    }
}