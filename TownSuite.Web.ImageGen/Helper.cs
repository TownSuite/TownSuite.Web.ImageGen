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
        var imageFormat = ImageFormat.GetFormat(image_format);

        if (imageFormat == ImageFormat.Format.jpeg)
        {
            await image.SaveAsync(ms, new JpegEncoder()
            {
                Quality = 85
            });
            extension = "jpeg";
            contentType = "image/jpeg";
        }
        else if (imageFormat == ImageFormat.Format.webp)
        {
            await image.SaveAsync(ms, new WebpEncoder());
            extension = "webp";
            contentType = "image/webp";
        }
        else if (imageFormat == ImageFormat.Format.gif)
        {
            await image.SaveAsync(ms, new GifEncoder());
            extension = "gif";
            contentType = "image/gif";
        }
        else if (imageFormat == ImageFormat.Format.avif && HeifEncoder.Available())
        {
            using (var context = new HeifContext())
            using (var heifImage = HeifEncoder.ConvertSharpToHeif(image.CloneAs<Rgba32>()))
            {
                var encoder = context.GetEncoder(HeifCompressionFormat.Av1);
                encoder.SetLossyQuality(85);
                context.EncodeImage(heifImage, encoder);
                context.WriteToStream(ms);
            }
            extension = "avif";
            contentType = "image/avif";
        }
        else if (imageFormat == ImageFormat.Format.avif && HeifEncoder.Available())
        {
            using (var context = new HeifContext())
            using (var heifImage = HeifEncoder.ConvertSharpToHeif(image.CloneAs<Rgba32>()))
            {
                var encoder = context.GetEncoder(HeifCompressionFormat.Hevc);
                encoder.SetLossyQuality(85); 
                context.EncodeImage(heifImage, encoder);
                context.WriteToStream(ms);
            }
            extension = "heic";
            contentType = "image/heic";
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