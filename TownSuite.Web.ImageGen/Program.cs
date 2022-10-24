using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Diagnostics;
using TownSuite.Web.ImageGen;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
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

app.MapGet("/avatar/{name}", async (ctx) =>
{
    var inst = new ImageProvider(new GenerateIdenticonImageRepo(), builder.Configuration,
        new ImageCacheProvider());
    var rMetaData = GetRequestMetaData(ctx, "avatar");
    var result = await inst.GetAsync(rMetaData);
    await WriteOutput(ctx, result);
});
app.MapGet("/placeholder/{name}", async (ctx) =>
{
    var inst = new ImageProvider(new GeneratePlaceholderImageRepo(), builder.Configuration,
        new ImageCacheProvider());
    var rMetaData = GetRequestMetaData(ctx, "placeholder");
    var result = await inst.GetAsync(rMetaData);
    await WriteOutput(ctx, result);
});


app.UseStaticFiles();

app.Run();

RequestMetaData GetRequestMetaData(HttpContext ctx, string folder)
{
    string id = Hash(ctx.Request.Path.Value.Split("/").LastOrDefault());
    string cacheFolder = builder.Configuration.GetValue<string>("CacheFolder");

    int width = 80;
    int height = 80;
    int maxWidth = builder.Configuration.GetValue<int>("MaxWidth");
    int maxHeight = builder.Configuration.GetValue<int>("MaxHeight");
    // identicons
    ctx.Request.Query.TryGetValue("s", out var strSize);
    int.TryParse(strSize, out var size);

    // placeholders
    if (ctx.Request.Query.ContainsKey("w"))
    {
        ctx.Request.Query.TryGetValue("w", out strSize);
        int.TryParse(strSize, out width);
    }

    if (ctx.Request.Query.ContainsKey("h"))
    {
        ctx.Request.Query.TryGetValue("h", out strSize);
        int.TryParse(strSize, out height);
    }

    string path;
    if (size == 0)
    {
        if (height > maxHeight || width > maxWidth)
        {
            throw new Exception($"Max allowable width is {maxWidth} and max allowable height is {maxHeight}");
        }

        path = Path.Combine(cacheFolder, folder, $"{id}_{width}_{height}.png");
        return new RequestMetaData(height, width, path,
            id);
    }

    if (size > maxHeight || size > maxWidth)
    {
        throw new Exception($"Max allowable width is {maxWidth} and max allowable height is {maxHeight}");
    }

    path = Path.Combine(cacheFolder, folder, $"{id}_{size}_{size}.png");
    return new RequestMetaData(size, size, path,
        id);
}

async Task WriteOutput(HttpContext ctx, (byte[] image, ImageMetaData metadata) result)
{
    ctx.Response.Headers.Add("Content-Type", "image/png");
    ctx.Response.Headers.Add("Expires", result.metadata.Expires.ToString());
    ctx.Response.Headers.Add("Cache-Control", "max-age=300");
    ctx.Response.Headers.Add("Content-Length", result.metadata.ContentLength.ToString());
    ctx.Response.Headers.Add("Last-Modified", result.metadata.LastModifiedUtc.ToUniversalTime().ToString("R"));
    ctx.Response.Headers.Add("Content-Disposition", $"inline; filename=\"{result.metadata.Filename}\"");
    await ctx.Response.Body.WriteAsync(result.image);
}

static string Hash(string nonHashedString)
{
    using var md5 = MD5.Create();
    byte[] data = System.Text.Encoding.UTF8.GetBytes(nonHashedString);
    byte[] retVal = md5.ComputeHash(data);
    return BitConverter.ToString(retVal);
}