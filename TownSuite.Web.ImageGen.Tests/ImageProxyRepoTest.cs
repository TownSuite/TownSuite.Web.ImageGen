using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;

namespace TownSuite.Web.ImageGen.Tests;

public class ImageProxyRepoTest
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public async Task Test1()
    {
        var downloader = new DownloaderFake();
        var proxyRepo = new ImageProxyRepo(downloader);
        var request = new ImageProxyRequestMetaData();
        var query = CreateContext(800, 800, "jpg");
        
        request.GetRequestMetaData(new Settings()
            {
                CacheFolder = "cache/folder/",
                MaxHeight = 1000,
                MaxWidth = 1000
            },
            query.query,
            query.rawQuery,
            "/proxy/assets%2Ffacility.jpg",
            "test_output");
        var results = await proxyRepo.Get(request);

        Assert.That(results.metadata.ContentType, Is.EqualTo("image/jpeg"));
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