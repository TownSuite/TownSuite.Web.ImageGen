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
    private readonly Settings _settings;

    public GenerateIdenticonImageRepo(Settings settings)
    {
        _settings = settings;
    }

    public async Task<(byte[] imageData, ImageMetaData metadata)> Get(RequestMetaData request)
    {
        using var ms = new MemoryStream();

        await Jdenticon.Identicon
            .FromValue(request.Id, size: request.Width)
            .SaveAsPngAsync(ms);

        ms.Seek(0, SeekOrigin.Begin);
        var img = await Image.LoadAsync(ms);
        var result = await Helper.BinaryAsBytes(img, request.ImageFormat);
        var imd = new ImageMetaData(DateTime.UtcNow, TimeSpan.FromMinutes(_settings.HttpCacheControlMaxAgeInMinutes),
            result.image.Length,
            $"{request.Id}.{request.ImageFormat}",
            result.contentType, request.Path);
        return (result.image, imd);
    }
}