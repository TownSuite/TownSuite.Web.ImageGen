
using LibHeifSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace TownSuite.Web.ImageGen.Tests;

[TestFixture]
public class HeifEncodeTest
{
    [TestCase("color-no-alpha.png",     800, 800),
     TestCase("grayscale-no-alpha.png", 800, 800),
     TestCase("grayscale-alpha.png",    400, 400),
     TestCase("color-alpha.png",        400, 400),
     TestCase("facility.jpg",           800, 365)]
    public async Task CanConvertSharpToHeif(string imageName, int width, int height)
    {
        var origImage = await Image.LoadAsync($"assets/{imageName}");
        Assert.That(origImage.Height, Is.EqualTo(height));
        Assert.That(origImage.Width, Is.EqualTo(width));

        using var context = new HeifContext();
        using HeifImage heifImage = HeifEncoder.ConvertSharpToHeif(origImage.CloneAs<Rgba32>());

        Assert.That(heifImage.Height, Is.EqualTo(height));
        Assert.That(heifImage.Width, Is.EqualTo(width));
    }

    [TestCase("color-no-alpha.png",     false, false),
     TestCase("grayscale-no-alpha.png", true,  false),
     TestCase("grayscale-alpha.png",    true,  true),
     TestCase("color-alpha.png",        false, true),
     TestCase("facility.jpg",           false, false)]
    public async Task CanAnalyzeImage(string imageName, bool expectGrayscale, bool expectTransparency)
    {
        var origImage = await Image.LoadAsync($"assets/{imageName}");
        (bool isGrayscale, bool hasTransparency) = HeifEncoder.AnalyzeImage(origImage.CloneAs<Rgba32>());
        Assert.That(isGrayscale, Is.EqualTo(expectGrayscale));
        Assert.That(hasTransparency, Is.EqualTo(expectTransparency));
    }
}
