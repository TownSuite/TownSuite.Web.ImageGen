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
builder.Services.AddScoped<Settings>(s => new Settings()
{
    CacheFolder = builder.Configuration.GetValue<string>("CacheFolder"),
    MaxHeight = builder.Configuration.GetValue<int>("MaxHeight"),
    MaxWidth = builder.Configuration.GetValue<int>("MaxWidth")
});
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
    var inst = new ImageProvider(new GenerateIdenticonImageRepo(),
        new ImageCacheProvider());
    var rMetaData =
        new RequestMetaData().GetRequestMetaData(config, ctx.Request.Query, ctx.Request.QueryString.Value,
            ctx.Request.Path.Value, "avatar");
    var result = await inst.GetAsync(rMetaData);
    await WriteOutput(ctx, result);
});
app.MapGet("/placeholder/{name}", async (HttpContext ctx, Settings config) =>
{
    var inst = new ImageProvider(new GeneratePlaceholderImageRepo(),
        new ImageCacheProvider());

    var rMetaData =
        new RequestMetaData().GetRequestMetaData(config, ctx.Request.Query, ctx.Request.QueryString.Value,
            ctx.Request.Path.Value, "placeholder");
    var result = await inst.GetAsync(rMetaData);
    await WriteOutput(ctx, result);
});
app.MapGet("/proxy/{name}", async (HttpContext ctx, IImageDownloader downloader, Settings config) =>
{
    var inst = new ImageProvider(new ImageProxyRepo(downloader),
        new ImageCacheProvider());
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

async Task WriteOutput(HttpContext ctx, (byte[] image, ImageMetaData metadata) result)
{
    ctx.Response.Headers.Add("Content-Type", result.metadata.ContentType);
    ctx.Response.Headers.Add("Expires", result.metadata.Expires.ToString());
    ctx.Response.Headers.Add("Cache-Control", "max-age=300");
    ctx.Response.Headers.Add("Content-Length", result.metadata.ContentLength.ToString());
    ctx.Response.Headers.Add("Last-Modified", result.metadata.LastModifiedUtc.ToUniversalTime().ToString("R"));
    ctx.Response.Headers.Add("Content-Disposition", $"inline; filename=\"{result.metadata.Filename}\"");
    await ctx.Response.Body.WriteAsync(result.image);
}