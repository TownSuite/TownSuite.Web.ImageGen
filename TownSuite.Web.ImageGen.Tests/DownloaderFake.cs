using SixLabors.ImageSharp;

namespace TownSuite.Web.ImageGen.Tests;

public class DownloaderFake : IImageDownloader
{
    readonly string _contentType;
    public DownloaderFake(string contentType)
    {
        _contentType = contentType;
    }
    public Task<(Stream S, string ContentType)> Download(string srcUrl)
    {
        Stream fs = new FileStream(srcUrl, FileMode.Open);
        return Task.FromResult((fs,_contentType));
    }
}