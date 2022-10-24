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
    public async Task<(byte[] imageData, ImageMetaData metadata)> Get(string id, int width, int height)
    {
        using var ms = new MemoryStream();
        await Jdenticon.Identicon
            .FromValue(id, size: width)
            .SaveAsPngAsync(ms);
        
        var md = new ImageMetaData(DateTime.UtcNow, TimeSpan.FromDays(360), ms.Length, $"{id}.png");
        return (ms.ToArray(), md);
    }
}