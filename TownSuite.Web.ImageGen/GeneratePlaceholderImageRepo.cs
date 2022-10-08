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

    public async Task<(byte[] imageData, ImageMetaData metadata)> Get(string id,
        int width, int height)
    {    
        var font = GetFont("Hack", 25);
        var img2 = await DrawText(id, font, Color.Aqua, Random.Shared, width, height);
        var md = new ImageMetaData(DateTime.UtcNow, TimeSpan.FromDays(360), img2.Length, $"{id}.png");
        return (img2, md);
    }

    static async Task<byte[]> DrawText(string text,
        SixLabors.Fonts.Font font,
        SixLabors.ImageSharp.Color textColor,
        Random randonGen, int imageWidth,
        int imageHeight)
    {
        using Image image = new Image<Rgba32>(imageWidth, imageHeight);

        // TODO: read background color from appsettings.json
       // Color randomColor = Color.FromRgb(Convert.ToByte(randonGen.Next(255)), Convert.ToByte(randonGen.Next(255)),
        //    Convert.ToByte(randonGen.Next(255)));
        Color randomColor = Color.Cornsilk;
        image.Mutate(x => x.Clear(randomColor));
        var location = new PointF(0, (int)(imageHeight / 2.5));
        image.Mutate(x => x.DrawText(text, font,
            textColor, location));

        return await BinaryAsBytes(image);
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
        if (family == null)
        {
            family = collection.Get("Hack");
        }

        return new Font(family, size);
    }

    static async Task<byte[]> BinaryAsBytes(Image image)
    {
        using var ms = new MemoryStream();
        await image.SaveAsync(ms, new PngEncoder());
        return ms.ToArray();
    }
}