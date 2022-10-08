using TownSuite.Web.ImageGen;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
var app = builder.Build();

app.MapGet("/avatar/{name}", async (ctx) =>
{
    var inst = new ImageProvider(new GenerateIdenticonImageRepo(), builder.Configuration,
        new ImageCacheProvider());
    var result = await inst.GetAsync(ctx);
    await WriteOutput(ctx, result);
});
app.MapGet("/placeholder/{name}", async (ctx) =>
{
    var inst = new ImageProvider(new GeneratePlaceholderImageRepo(), builder.Configuration,
        new ImageCacheProvider());
    var result = await inst.GetAsync(ctx);
    await WriteOutput(ctx, result);
});

app.UseStaticFiles();

app.Run();

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