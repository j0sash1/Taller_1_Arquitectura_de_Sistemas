using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;
using Shortly.Application.Interfaces;
using Shortly.Application.Services;
using Shortly.Endpoints;
using Shortly.Infrastructure;
using Shortly.Infrastructure.Persistence;
using Shortly.Infrastructure.Repositories;
using Shortly.Middleware;
using System.IO.Compression;
using System.Threading.RateLimiting;

// Creates the ASP.NET Core application builder with initial configuration
var builder = WebApplication.CreateBuilder(args);

// Configures Serilog as the global bootstrap logger, reading all settings from appsettings.json
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

// Tells the host to use Serilog as its logging system
builder.Host.UseSerilog();

// Registers Razor Pages services
builder.Services.AddRazorPages(options =>
{
    // Applies the "login" rate limiting policy (see AddRateLimiter below)
    // only to the Login page, not to every Razor Page.
    options.Conventions.AddPageApplicationModelConvention("/Login", model =>
    {
        model.EndpointMetadata.Add(new EnableRateLimitingAttribute("login"));
    });
});

// Registers the OpenAPI document generator with version 3.1 and API metadata
builder.Services.AddOpenApi(options =>
{
    options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "Shortly API",
            Description = "A URL shortener service with user authentication and link management.",
            Version = "v1"
        };
        return Task.CompletedTask;
    });
});

// Registers the SQLite database context using Entity Framework Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("AppDbContext")));

// Configures a volatile server-side ticket store (auth state lost on restart)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSingleton<MemoryCacheTicketStore>();

// Configures cookie authentication with a server-side ticket store
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Error";

        // Cookie hardening: HttpOnly hides the cookie from JS (blocks theft
        // via XSS); SameSite=Strict never sends it on cross-site requests
        // (blocks CSRF); Secure requires HTTPS in production so it can't be
        // read over plain HTTP.
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.Path = "/";
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
    });

// Injects the ticket store into the cookie options after the service provider is built
builder.Services.AddSingleton<IConfigureOptions<CookieAuthenticationOptions>>(sp =>
{
    var store = sp.GetRequiredService<MemoryCacheTicketStore>();
    return new ConfigureNamedOptions<CookieAuthenticationOptions>(
        CookieAuthenticationDefaults.AuthenticationScheme,
        options => options.SessionStore = store);
});

// Registers the authorization service
builder.Services.AddAuthorization();

// Configures a fixed-window rate limiting policy
// The policy is applied to the Razor Pages endpoint group to reduce
// abusive request rates and returns HTTP 429 with Retry-After when exceeded
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("login", policy =>
    {
        policy.PermitLimit = 10;
        policy.Window = TimeSpan.FromMinutes(5);
        policy.QueueLimit = 0;
        policy.AutoReplenishment = true;
    });

    options.OnRejected = async (context, _) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "300";

        await context.HttpContext.Response.WriteAsync(
            "Too many requests. Please try again later.");
    };
});

// Enables response compression with Brotli and Gzip.
// HTTP allows a client to advertise the codecs it understands via
// Accept-Encoding; the server picks one, compresses the body, and reports
// the choice in Content-Encoding. This shrinks compressible payloads
// (HTML pages, JSON, CSS, JS) and reduces transfer time, especially on
// slow connections. Brotli is preferred (better ratio); Gzip is kept as a
// fallback for clients that only support it.
builder.Services.AddResponseCompression(options =>
{
    // Response compression is disabled for HTTPS by default because mixing
    // dynamic, secret-bearing content (e.g. reflected tokens) with
    // compression can leak information through response-size side channels
    // (the BREACH-style attack). Shortly does not reflect secrets in its
    // compressible responses, so it is safe to enable it here.
    options.EnableForHttps = true;

    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();

    // Reuses the framework's default MIME type allow-list (text/*,
    // application/json, application/xml, image/svg+xml, etc.). Binary
    // formats such as images or fonts are intentionally left out: they are
    // already compressed, so re-compressing them wastes CPU with no
    // bandwidth benefit.
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes;
});

// "Fastest" trades a slightly larger payload for lower CPU usage per
// request, which is the right default for a low-traffic app like Shortly.
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

// Restrictive CORS policy: only allows the trusted client origin to
// call this API cross-origin. Everything else is blocked by the browser.
builder.Services.AddCors(options =>
{
    options.AddPolicy("ShortlyClient", policy =>
    {
        // Replace with the real frontend origin in production.
        policy.WithOrigins("http://localhost:3000")
              .WithMethods("GET")
              .WithHeaders("Content-Type");
    });
});

// Registers repositories and services for dependency injection (scoped lifetime)
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ILinkRepository, LinkRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ILinkService, LinkService>();

// Builds the application with all registered configurations
var app = builder.Build();

// In non-development environments, uses a friendly error page
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

// Redirects HTTP requests to HTTPS automatically
// app.UseHttpsRedirection();

// Applies response compression (see AddResponseCompression above). Must run
// early in the pipeline, before the middleware that actually writes the
// response body (static files, Razor Pages, minimal API endpoints), so it
// can wrap and compress whatever they produce.
app.UseResponseCompression();

// Serves static files from the wwwroot/ folder
app.UseStaticFiles();

// Enables request routing
app.UseRouting();

// Enables CORS. Must go after UseRouting and before UseAuthorization.
// The policy itself is applied per-endpoint (see RequireCors), not here.
app.UseCors();

// Enables the ASP.NET Core rate limiting middleware
app.UseRateLimiter();

// Adds baseline security headers to every HTTP response.
app.UseMiddleware<SecurityHeadersMiddleware>();

// Measures request execution time for diagnostics.
app.UseMiddleware<ResponseTimingMiddleware>();

// Enables authentication (must come after UseRouting)
app.UseAuthentication();

// Enables authorization (must come after UseAuthentication)
app.UseAuthorization();

// Maps static assets with automatic versioning
app.MapStaticAssets();

// Maps Razor Pages. The "login" rate limiting policy is scoped to just
// the Login page via the AddPageApplicationModelConvention above.
app.MapRazorPages()
    .WithStaticAssets();

// Exposes the OpenAPI document at /openapi/v1.json
app.MapOpenApi();

// Serves the Scalar interactive API reference UI at /scalar/v1
app.MapScalarApiReference();

// Maps the redirect endpoint GET /{shortUrl} from Endpoints/UrlRedirectEndpoint.cs
app.MapUrlRedirect();

// Creates a scope for scoped services (e.g. AppDbContext)
using (var scope = app.Services.CreateScope())
{
    // Gets the database context from the DI container
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Creates the database and tables if they do not exist
    db.Database.EnsureCreated();
    // Reads the admin password from configuration or uses a default value
    var seedPassword = app.Configuration["Seed:AdminPassword"] ?? "admin123";
    // Seeds initial data (admin user and sample links)
    await DbInitializer.InitializeAsync(db, seedPassword);
}

// Starts the application and begins listening for HTTP requests
await app.RunAsync();