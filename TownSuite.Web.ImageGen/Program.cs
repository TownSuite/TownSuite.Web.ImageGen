using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Primitives;
using TownSuite.Web.ImageGen;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddHttpClient();
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
    UserAgent = builder.Configuration.GetValue<string>("UserAgent")
});

builder.Services.AddHostedService<BackgroundWorkerService>();

var app = builder.Build();

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

            var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
            await context.Response.WriteAsync(exceptionHandlerPathFeature?.Error.Message ?? "");
        });
    });
}

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
app.MapHealthChecks("/healthz");
app.Run();

async Task WriteOutput(HttpContext ctx, ImageMetaData metadata)
{
    ctx.Response.Headers.Add("Content-Type", metadata.ContentType);
    ctx.Response.Headers.Add("Expires",  DateTime.UtcNow.Add(metadata.Expires).ToString("R"));
    ctx.Response.Headers.Add("Cache-Control", $"max-age={metadata.Expires.TotalSeconds}");
    ctx.Response.Headers.Add("Content-Length", metadata.ContentLength.ToString());
    ctx.Response.Headers.Add("Last-Modified", metadata.LastModifiedUtc.ToUniversalTime().ToString("R"));
    ctx.Response.Headers.Add("Content-Disposition", $"inline; filename=\"{metadata.Filename}\"");
    //await ctx.Response.Body.WriteAsync(result.image);

    // *************************** attempt just streams
    // inspiration https://ticehurst.com/2022/01/30/blob-streaming.html
    // Support range requests if it specifies exactly one range with a from position specified

    await using var fs = new FileStream(metadata.FullFilePath, FileMode.Open);
    await fs.CopyToAsync(ctx.Response.Body);
    
  /*
    var ranges = ctx.Request.GetTypedHeaders().Range?.Ranges;
    if (ranges != null && ranges.Count == 1 && ranges.First().From.HasValue)
    {
        var range = ranges.First();
        long? length = range.To.HasValue ? range.To.Value - range.From!.Value + 1 : null;
        blobRange = new(range.From!.Value, length);
    }
    
    using (var fs = new FileStream("valid-path-to-file-on-server.txt", FileMode.Open))
    {
        fs.
        await fs.CopyToAsync(ctx.Response.Body);
    }
    
    await ctx.Response.SendFileAsync(new FileInfo(), 0, 0);
    */
}