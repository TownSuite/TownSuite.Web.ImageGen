namespace TownSuite.Web.ImageGen;

public interface IImageCacheProvider
{
    Task<(byte[] image, ImageMetaData metadata)> GetAsync(string path);
    Task Save(byte[] image, string path);
}