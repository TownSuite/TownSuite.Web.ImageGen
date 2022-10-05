using Microsoft.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Web.Caching;
using SixLabors.ImageSharp.Web.DependencyInjection;
using TownSuite.Web.Avatars;

var builder = WebApplication.CreateBuilder(args);
//builder.WebHost.UseSetting("WebRoot",Path.Combine(System.Environment.CurrentDirectory, "..", "wwwroot"));
//builder.WebHost
//    .UseWebRoot(Path.Combine(System.Environment.CurrentDirectory, "..", "wwwroot"));
builder.Services.AddControllers();
builder.Services.AddSingleton<IImageRepository, GenerateImageRepo>();
// https://docs.sixlabors.com/articles/imagesharp.web/gettingstarted.html
builder.Services.AddImageSharp(
        options =>
        {
            options.BrowserMaxAge = TimeSpan.FromDays(7);
            options.CacheMaxAge = TimeSpan.FromDays(365);
        }).Configure<PhysicalFileSystemCacheOptions>(options =>
    {
        // TODO: read from appsettings.json
        options.CacheFolder = "different-cache";
        // TODO: read from appsettings.json
        options.CacheRootPath = "image-cache";
    })
    .ClearProviders()
    .AddProvider<CustomImageProvider>();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");


app.UseImageSharp();

app.UseStaticFiles();

app.Run();