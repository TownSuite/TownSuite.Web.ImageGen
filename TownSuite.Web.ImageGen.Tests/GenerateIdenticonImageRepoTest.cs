using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;
using SixLabors.ImageSharp;

namespace TownSuite.Web.ImageGen.Tests;

[TestFixture]
public class GenerateIdenticonImageRepoTest
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public async Task Test1()
    {
        var origImage = await Image.LoadAsync("assets/facility.jpg");
        Assert.That(origImage.Height, Is.EqualTo(365));
        Assert.That(origImage.Width, Is.EqualTo(800));
        
        var downloader = new DownloaderFake();
        var repo = new GenerateIdenticonImageRepo();
        var request = new RequestMetaData();
        var query = CreateContext(777, 777, "webp");
        
        request.GetRequestMetaData(new Settings()
            {
                CacheFolder = "cache/folder/",
                MaxHeight = 1000,
                MaxWidth = 1000
            },
            query.query,
            query.rawQuery,
            "/avatar/hello",
            "test_output");
        var results = await repo.Get(request);

        using var ms = new MemoryStream(results.imageData);
        var newImage = await Image.LoadAsync(ms);
        Assert.That(results.metadata.ContentType, Is.EqualTo("image/webp"));
        Assert.That(newImage.Height, Is.EqualTo(777));
        Assert.That(newImage.Width, Is.EqualTo(777));
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