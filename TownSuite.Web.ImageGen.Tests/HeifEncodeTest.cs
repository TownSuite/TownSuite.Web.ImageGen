
using LibHeifSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace TownSuite.Web.ImageGen.Tests;

[TestFixture]
public class HeifEncodeTest
{
    [Test]
    public async Task CanConvertSharpToHeif()
    {
        var origImage = await Image.LoadAsync("assets/facility.jpg");
        Assert.That(origImage.Height, Is.EqualTo(365));
        Assert.That(origImage.Width, Is.EqualTo(800));

        HeifImage image = HeifEncoder.ConvertSharpToHeif(origImage.CloneAs<Rgba32>());

        Assert.That(image.Height, Is.EqualTo(365));
        Assert.That(image.Width, Is.EqualTo(800));
    }

    [TestCase("color-no-alpha.png", false, false),
     TestCase("grayscale-no-alpha.png", true, false),
     TestCase("grayscale-alpha.png", true, true),
     TestCase("color-alpha.png", false, true)]
    public async Task CanAnalyzeImage(string imageName, bool expectGrayscale, bool expectTransparency)
    {
        var origImage = await Image.LoadAsync($"assets/{imageName}");
        (bool isGrayscale, bool hasTransparency) = HeifEncoder.AnalyzeImage(origImage.CloneAs<Rgba32>());
        Assert.That(isGrayscale, Is.EqualTo(expectGrayscale));
        Assert.That(hasTransparency, Is.EqualTo(expectTransparency));
    }
}
