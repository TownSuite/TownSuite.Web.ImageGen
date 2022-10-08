namespace TownSuite.Web.ImageGen;

public interface IImageRepository
{
    string Folder { get; }
    Task<(byte[] imageData, ImageMetaData metadata)> Get(string id, int width, int height);
}