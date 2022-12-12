using System.Net.Mime;
using Jdenticon;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace TownSuite.Web.ImageGen;

public class GenerateIdenticonImageRepo : IImageRepository
{
    public string Folder { get; } = "avatars";

    public async Task<(byte[] imageData, ImageMetaData metadata)> Get(RequestMetaData request)
    {
        using var ms = new MemoryStream();

        await Jdenticon.Identicon
            .FromValue(request.Id, size: request.Width)
            .SaveAsPngAsync(ms);

        ms.Seek(0, SeekOrigin.Begin);
        var img = await Image.LoadAsync(ms);
        var result = await Helper.BinaryAsBytes(img, request.ImageFormat);
        var imd = new ImageMetaData(DateTime.UtcNow, TimeSpan.FromDays(360), result.image.Length,
            $"{request.Id}.{request.ImageFormat}",
            result.contentType);
        return (result.image, imd);
    }
}