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
builder.Services.AddRazorPages();

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
// Configures Brotli (preferred) and Gzip to reduce response payloads.
builder.Services.AddResponseCompression(options =>
{
    // Enabled for HTTPS: Safe in this app because it doesn't reflect 
    // dynamic secrets (mitigating BREACH-style attacks).
    options.EnableForHttps = true;

    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();

    // Compresses text/JSON only. Ignores binary formats (images, fonts) 
    // that are already compressed to prevent wasting CPU.
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes;
});

// "Fastest" level minimizes CPU usage per request, ideal for low-traffic apps.
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
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

// Enables compression. Must run before the middlewares that write the response body.
app.UseResponseCompression();

// Serves static files from the wwwroot/ folder
app.UseStaticFiles();

// Enables request routing
app.UseRouting();

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

// Maps Razor Pages and enforces the "login" rate limiting policy
app.MapRazorPages()
    .RequireRateLimiting("login")
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