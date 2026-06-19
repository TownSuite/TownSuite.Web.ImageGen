using System.Net;
using System.Security.Cryptography;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Primitives;
using TownSuite.Web.ImageGen;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddHttpClient();
// SSRF-hardened client used by the image proxy: no redirect following and a
// connect-time guard that blocks private/loopback/link-local/metadata addresses.
builder.Services.AddHttpClient("imageproxy")
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        // Disable any ambient/system HTTP proxy. Otherwise requests would be tunneled to
        // the proxy and the connect-time IP checks would validate the proxy's address
        // instead of the real destination, bypassing the SSRF guard.
        UseProxy = false,
        ConnectCallback = SsrfGuard.ConnectCallback
    })
    .ConfigureHttpClient((sp, client) =>
    {
        var s = sp.GetRequiredService<Settings>();
        client.Timeout = TimeSpan.FromSeconds(s.ProxyTimeoutSeconds > 0 ? s.ProxyTimeoutSeconds : 30);
    });
builder.Services.AddScoped<IImageDownloader, ImageDownloader>();
builder.Services.AddSingleton<Settings>(s => new Settings()
{
    CacheFolder = builder.Configuration.GetValue<string>("CacheFolder"),
    MaxHeight = builder.Configuration.GetValue<int>("MaxHeight"),
    CacheBackgroundCleanupTimerSeconds = builder.Configuration.GetValue<int>("CacheBackgroundCleanupTimerSeconds"),
    CacheMaxLifeTimeMinutes = builder.Configuration.GetValue<int>("CacheMaxLifeTimeMinutes"),
    CacheSizeLimitInMiB = builder.Configuration.GetValue<int>("CacheSizeLimitInMiB"),
    HttpCacheControlMaxAgeInMinutes = builder.Configuration.GetValue<int>("HttpCacheControlMaxAgeInMinutes"),
    MaxWidth = builder.Configuration.GetValue<int>("MaxWidth"),
    UserAgent = builder.Configuration.GetValue<string>("UserAgent"),
    MaxDownloadSizeInMiB = builder.Configuration.GetValue<int>("MaxDownloadSizeInMiB"),
    ProxyTimeoutSeconds = builder.Configuration.GetValue<int>("ProxyTimeoutSeconds"),
    MaxSourceImagePixels = builder.Configuration.GetValue<long>("MaxSourceImagePixels")
});

// Rate limiting (DoS / abuse protection). Partitioned by client IP with a fixed window.
// NOTE: behind a reverse proxy / k8s ingress, enable ForwardedHeaders middleware with a
// trusted-proxy configuration so RemoteIpAddress reflects the real client rather than the
// proxy; otherwise all traffic shares one partition.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var key = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 60,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    });
});

builder.Services.AddHostedService<BackgroundWorkerService>();

var app = builder.Build();

// Ensure the cache directory exists at startup so the background cleanup service and the
// cache writer don't error on first run — important for local / non-Docker deployments
// where the configured path (e.g. relative "cache") may not exist yet.
var startupSettings = app.Services.GetRequiredService<Settings>();
if (!string.IsNullOrWhiteSpace(startupSettings.CacheFolder))
{
    Directory.CreateDirectory(startupSettings.CacheFolder);
}

if (builder.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "text/plain";
            // UseExceptionHandler clears headers set by downstream middleware, so re-apply
            // the nosniff header here to keep it on error responses too.
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";

            // Do NOT echo the exception message to the client: it can leak internal
            // details (cache paths, resolved internal IPs from SSRF checks, downstream
            // hostnames) and acts as a recon oracle. Log the real error server-side and
            // return a generic message instead.
            var feature = context.Features.Get<IExceptionHandlerPathFeature>();
            if (feature?.Error is not null)
            {
                var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(feature.Error, "Unhandled error processing {Path}", feature.Path);
            }

            await context.Response.WriteAsync("An error occurred while processing the request.");
        });
    });
}

// Defense-in-depth: prevent MIME-type sniffing on every response.
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    await next();
});

app.UseRateLimiter();

app.MapGet("/avatar/{name}", async (HttpContext ctx, Settings config) =>
{
    var inst = new ImageProvider(new GenerateIdenticonImageRepo(config),
        new ImageCacheProvider(config));
    var rMetaData =
        new RequestMetaData().GetRequestMetaData(config, ctx.Request.Query, ctx.Request.QueryString.Value,
            ctx.Request.Path.Value, "avatar");
    var result = await inst.GetAsync(rMetaData);
    await WriteOutput(ctx, result);
});
app.MapGet("/placeholder/{name}", async (HttpContext ctx, Settings config) =>
{
    var inst = new ImageProvider(new GeneratePlaceholderImageRepo(config),
        new ImageCacheProvider(config));

    var rMetaData =
        new RequestMetaData().GetRequestMetaData(config, ctx.Request.Query, ctx.Request.QueryString.Value,
            ctx.Request.Path.Value, "placeholder");
    var result = await inst.GetAsync(rMetaData);
    await WriteOutput(ctx, result);
});
app.MapGet("/proxy/{name}", async (HttpContext ctx, IImageDownloader downloader, Settings config) =>
{
    var inst = new ImageProvider(new ImageProxyRepo(downloader, config),
        new ImageCacheProvider(config));
    var rMetaData =
        new ImageProxyRequestMetaData().GetRequestMetaData(config, ctx.Request.Query, ctx.Request.QueryString.Value,
            ctx.Request.Path.Value,
            "imageproxy");
    var result = await inst.GetAsync(rMetaData);
    await WriteOutput(ctx, result);
});

app.UseStaticFiles();
// Exempt health checks from rate limiting so liveness/readiness probes are never
// throttled (a 429 to the kubelet would trigger spurious restarts).
app.MapHealthChecks("/healthz").DisableRateLimiting();
app.Run();

async Task WriteOutput(HttpContext ctx, ImageMetaData metadata)
{
    var headers = ctx.Response.Headers;
    headers["Content-Type"] = metadata.ContentType;
    headers["Expires"] = DateTime.UtcNow.Add(metadata.Expires).ToString("R");
    headers["Cache-Control"] = $"max-age={(long)metadata.Expires.TotalSeconds}";
    headers["Content-Length"] = metadata.ContentLength.ToString();
    headers["Last-Modified"] = metadata.LastModifiedUtc.ToUniversalTime().ToString("R");

    // SVG can carry active content (scripts / event handlers). Force a download and
    // sandbox it so it is never rendered inline in the service's origin (stored-XSS
    // prevention). Other image types are safe to display inline.
    bool isSvg = metadata.ContentType is not null &&
                 metadata.ContentType.Contains("svg", StringComparison.OrdinalIgnoreCase);
    if (isSvg)
    {
        headers["Content-Disposition"] = $"attachment; filename=\"{metadata.Filename}\"";
        headers["Content-Security-Policy"] = "default-src 'none'; sandbox";
    }
    else
    {
        headers["Content-Disposition"] = $"inline; filename=\"{metadata.Filename}\"";
    }

    await using var fs = new FileStream(metadata.FullFilePath, FileMode.Open, FileAccess.Read);
    await fs.CopyToAsync(ctx.Response.Body);
}