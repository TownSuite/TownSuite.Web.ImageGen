using SixLabors.ImageSharp;

namespace TownSuite.Web.ImageGen.Tests;

public class DownloaderFake : IImageDownloader
{
    public async Task<Image> Download(string srcUrl)
    {
        return await Image.LoadAsync(srcUrl);
    }
}