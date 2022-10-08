using System.Security.Cryptography;
using Microsoft.Extensions.Primitives;
using TownSuite.Web.ImageGen;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
var app = builder.Build();

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
    
    int size = 0;
    int width = 80;
    int height = 80;
    StringValues strSize;

    // identicons
    ctx.Request.Query.TryGetValue("s", out strSize);
    int.TryParse(strSize, out size);

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
        path = System.IO.Path.Combine(cacheFolder, folder, $"{id}_{width}_{height}.png");
        return new RequestMetaData(height, width, path,
            id);
    }
     path = System.IO.Path.Combine(cacheFolder, folder, $"{id}_{size}_{size}.png");
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