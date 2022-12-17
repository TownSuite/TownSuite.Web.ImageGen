namespace TownSuite.Web.ImageGen;

public interface IImageCacheProvider
{
    Task<ImageMetaData?> GetAsync(RequestMetaData rMetaData);
    Task Save(byte[] image, RequestMetaData rMetaData);
}