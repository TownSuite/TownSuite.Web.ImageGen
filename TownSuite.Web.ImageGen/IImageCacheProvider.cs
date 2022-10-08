namespace TownSuite.Web.ImageGen;

public interface IImageCacheProvider
{
    Task<(byte[] image, ImageMetaData metadata)> GetAsync(RequestMetaData rMetaData);
    Task Save(byte[] image, RequestMetaData rMetaData);
}