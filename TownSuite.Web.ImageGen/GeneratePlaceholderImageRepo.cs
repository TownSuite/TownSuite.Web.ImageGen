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

public class GeneratePlaceholderImageRepo : IImageRepository
{
    public string Folder { get; } = "placeholders";

    private readonly Settings _settings;

    public GeneratePlaceholderImageRepo(Settings settings)
    {
        _settings = settings;
    }

    public async Task<(byte[] imageData, ImageMetaData metadata)> Get(RequestMetaData request)
    {
        var font = GetFont("Hack", 25);
        var img2 = await DrawText(request.Id, font, Color.Aqua, Random.Shared, request);
        var md = new ImageMetaData(DateTime.UtcNow, TimeSpan.FromMinutes(_settings.HttpCacheControlMaxAgeInMinutes),
            img2.image.Length,
            $"{request.Id}.{img2.fileExt}",
            img2.contentType, request.Path);
        return (img2.image, md);
    }

    static async Task<(byte[] image, string fileExt, string contentType)> DrawText(string text,
        SixLabors.Fonts.Font font,
        SixLabors.ImageSharp.Color textColor,
        Random randonGen, RequestMetaData request)
    {
        using Image image = new Image<Rgba32>(request.Width, request.Height);
        Color randomColor = Color.FromRgb(Convert.ToByte(randonGen.Next(255)), Convert.ToByte(randonGen.Next(255)),
            Convert.ToByte(randonGen.Next(255)));
        image.Mutate(x => x.Clear(randomColor));
        var location = new PointF(0, (int)(request.Height / 2.5));
        image.Mutate(x => x.DrawText(text, font,
            textColor, location));

        return await Helper.BinaryAsBytes(image, request.ImageFormat);
    }

    static FontCollection collection = null;

    /// <summary>
    /// Search for a font.  The following are valid values.  Invalid values will result in font family 'Hack'
    /// being used.
    /// Hack
    /// </summary>
    /// <param name="fontName"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    static SixLabors.Fonts.Font GetFont(string fontName, int size)
    {
        if (collection == null)
        {
            collection = new FontCollection();
            collection.Add("fonts/Hack-Bold.ttf");
            collection.Add("fonts/Hack-BoldItalic.ttf");
            collection.Add("fonts/Hack-Italic.ttf");
            collection.Add("fonts/Hack-Regular.ttf");
        }

        FontFamily family;
        collection.TryGet(fontName, out family);
        if (string.IsNullOrWhiteSpace(family.Name))
        {
            family = collection.Get("Hack");
        }

        return new Font(family, size);
    }
}