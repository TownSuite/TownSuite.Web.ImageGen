using SixLabors.ImageSharp;

namespace TownSuite.Web.ImageGen;

public interface IImageDownloader
{
    Task<Image> Download(string srcUrl);
}