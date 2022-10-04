using Microsoft.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Web.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// https://docs.sixlabors.com/articles/imagesharp.web/gettingstarted.html
builder.Services.AddImageSharp(
    options =>
    {
        options.BrowserMaxAge = TimeSpan.FromDays(7);
        options.CacheMaxAge = TimeSpan.FromDays(365);
    });

var app = builder.Build();

app.MapGet("/", () => "Hello World!");


app.UseImageSharp();

app.UseStaticFiles();

app.Run();