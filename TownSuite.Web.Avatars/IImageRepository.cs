using SixLabors.ImageSharp.Web;

namespace TownSuite.Web.Avatars;

public interface IImageRepository
{
     Task<(byte[] data, ImageMetadata metadata)> Get(string id);
}