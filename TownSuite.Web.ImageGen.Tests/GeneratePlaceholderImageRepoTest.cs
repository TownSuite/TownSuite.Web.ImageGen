using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using SixLabors.ImageSharp;

namespace TownSuite.Web.ImageGen.Tests;

[TestFixture]
public class GeneratePlaceHolderImageRepoTest
{
    [SetUp]
    public void Setup()
    {
    }

    private static string[] ImageFormatCases = new string[] { "jpeg", "png", "gif", "webp", "avif", "heic" };
    
    [Test, TestCaseSource("ImageFormatCases")]
    public async Task Test1(string imageformat)
    {
        var origImage = await Image.LoadAsync("assets/facility.jpg");
        Assert.That(origImage.Height, Is.EqualTo(365));
        Assert.That(origImage.Width, Is.EqualTo(800));
        
        var downloader = new DownloaderFake("image/jpeg");
        var repo = new GeneratePlaceholderImageRepo(new Settings()
        {
            HttpCacheControlMaxAgeInMinutes = 5
        });
        var request = new RequestMetaData();
        var query = CreateContext(555, 555, imageformat);
        
        request.GetRequestMetaData(new Settings()
            {
                CacheFolder = "cache/folder/",
                MaxHeight = 1000,
                MaxWidth = 1000
            },
            query.query,
            query.rawQuery,
            "/placeholder/hello",
            "test_output");
        var results = await repo.Get(request);
        Assert.That(results.metadata.ContentType, Is.EqualTo($"image/{imageformat}"));
        using var ms = new MemoryStream(results.imageData);
        Image newImage;
        if (ImageFormat.IsFormat(imageformat, ImageFormat.Format.avif)
            || ImageFormat.IsFormat(imageformat, ImageFormat.Format.heic))
        {
            newImage = HeifDecoder.ConvertHeifToSharp(ms);
        }
        else
        {
            newImage = await Image.LoadAsync(ms);
        }
        Assert.That(newImage.Height, Is.EqualTo(555));
        Assert.That(newImage.Width, Is.EqualTo(555));
    }

    private (QueryCollection query, string rawQuery) CreateContext(int h, int w,string imgformat)
    {
        var queries = new Dictionary<string, StringValues>
        {
            { "h", h.ToString() },
            { "w", w.ToString() },
            { "imgformat", imgformat }
        };

        
        return (new QueryCollection(queries), $"?h={h}&w={w}&imgformat={imgformat}");
    }
}