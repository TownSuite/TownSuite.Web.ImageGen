using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;

namespace TownSuite.Web.ImageGen;

public static class Helper
{
    public static async Task<(byte[] image, string fileExt, string contentType)> BinaryAsBytes(Image image,
        string image_format)
    {
        string contentType;
        string extension;
        using var ms = new MemoryStream();
        if (string.Equals(image_format, "png", StringComparison.InvariantCultureIgnoreCase))
        {
            await image.SaveAsync(ms, new PngEncoder());
            extension = "png";
            contentType = "image/png";
        }
        else
        {
            await image.SaveAsync(ms, new JpegEncoder());
            extension = "jpeg";
            contentType = "image/jpeg";
        }

        return (ms.ToArray(), extension, contentType);
    }
}