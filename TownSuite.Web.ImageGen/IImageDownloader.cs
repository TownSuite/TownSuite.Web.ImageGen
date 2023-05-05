using SixLabors.ImageSharp;

namespace TownSuite.Web.ImageGen;

public interface IImageDownloader
{
    Task<(Stream S, string ContentType)> Download(string srcUrl);
}