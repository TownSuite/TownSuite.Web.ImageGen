using LibHeifSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;

namespace TownSuite.Web.ImageGen;

public static class Helper
{
    public static async Task<(byte[] image, string fileExt, string contentType)> BinaryAsBytes(Image image,
        string image_format)
    {
        string contentType;
        string extension;
        using var ms = new MemoryStream();

        if (string.Equals(image_format, "jpg", StringComparison.InvariantCultureIgnoreCase)
            || string.Equals(image_format, "jpeg", StringComparison.InvariantCultureIgnoreCase))
        {
            await image.SaveAsync(ms, new JpegEncoder()
            {
                Quality = 85
            });
            extension = "jpeg";
            contentType = "image/jpeg";
        }
        else if (string.Equals(image_format, "webp", StringComparison.InvariantCultureIgnoreCase))
        {
            await image.SaveAsync(ms, new WebpEncoder());
            extension = "webp";
            contentType = "image/webp";
        }
        else if (string.Equals(image_format, "gif", StringComparison.InvariantCultureIgnoreCase))
        {
            await image.SaveAsync(ms, new GifEncoder());
            extension = "gif";
            contentType = "image/gif";
        }
        else if (string.Equals(image_format, "avif", StringComparison.InvariantCultureIgnoreCase))
        {
            using (var context = new HeifContext())
            using (var heifImage = ImageConversion.ConvertToHeifImage(image.CloneAs<Rgba32>(), premultiplyAlpha: false))
            {
                var encoder = context.GetEncoder(HeifCompressionFormat.Av1);
                encoder.SetLossyQuality(85); // Adjust quality as needed

                context.EncodeImage(heifImage, encoder);

                // Write the HeifImage to a temporary file
                var tempFilePath = Path.GetRandomFileName();
                context.WriteToFile(tempFilePath);

                // Read the temporary file into the MemoryStream
                using (var fileStream = File.OpenRead(tempFilePath))
                {
                    await fileStream.CopyToAsync(ms);
                }

                // Delete the temporary file
                File.Delete(tempFilePath);
            }

            extension = "avif";
            contentType = "image/avif";
        }
        else if (string.Equals(image_format, "heif", StringComparison.InvariantCultureIgnoreCase))
        {
            using (var context = new HeifContext())
            using (var heifImage = ImageConversion.ConvertToHeifImage(image.CloneAs<Rgba32>(), premultiplyAlpha: false))
            {
                var encoder = context.GetEncoder(HeifCompressionFormat.Hevc);
                encoder.SetLossyQuality(85); // Adjust quality as needed

                context.EncodeImage(heifImage, encoder);

                // Write the HeifImage to a temporary file
                var tempFilePath = Path.GetRandomFileName();
                context.WriteToFile(tempFilePath);
      

                // Read the temporary file into the MemoryStream
                using (var fileStream = File.OpenRead(tempFilePath))
                {
                    await fileStream.CopyToAsync(ms);
                }

                // Delete the temporary file
                File.Delete(tempFilePath);
            }

            extension = "heif";
            contentType = "image/heif";
        }
        else
        {
            await image.SaveAsync(ms, new PngEncoder());
            extension = "png";
            contentType = "image/png";
        }
        
        return (ms.ToArray(), extension, contentType);
    }
}