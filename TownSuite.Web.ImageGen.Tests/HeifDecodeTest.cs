using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace TownSuite.Web.ImageGen.Tests;

[TestFixture]
public class HeifDecodeTest
{
    [TestCase("avif" ,400, 400),
     TestCase("heic", 400, 400)]
    public async Task CanConvertHeifToSharp(string imageformat, int width, int height)
    {
        Image image = HeifDecoder.ConvertHeifToSharp(new MemoryStream(await File.ReadAllBytesAsync($"assets/{imageformat}_test.{imageformat}")));

        Assert.That(image.Height, Is.EqualTo(height));
        Assert.That(image.Width, Is.EqualTo(width));
    }

    [Test]
    public async Task ShouldCreateBlankImageOnEmptyStream()
    { 
        Image<Rgba32> image = HeifDecoder.ConvertHeifToSharp(new MemoryStream());

        Assert.That(image.Height, Is.EqualTo(1));
        Assert.That(image.Width, Is.EqualTo(1));

        Assert.That(image[0, 0].R, Is.EqualTo(0));
    }
}