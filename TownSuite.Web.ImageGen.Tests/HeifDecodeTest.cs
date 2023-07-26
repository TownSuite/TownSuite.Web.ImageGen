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
        var imageName = $"assets/{imageformat}_test.{imageformat}";
        Image image = HeifDecoder.ConvertHeifToSharp(new MemoryStream(await File.ReadAllBytesAsync(imageName)));
        Assert.Multiple(() =>
        {
            Assert.That(image.Height, Is.EqualTo(height), $"Photo {imageName} height incorrect");
            Assert.That(image.Width, Is.EqualTo(width), $"Photo {imageName} width incorrect");
        });
    }

    [Test]
    public void ShouldCreateBlankImageOnEmptyStream()
    {
        Image<Rgba32> image = HeifDecoder.ConvertHeifToSharp(new MemoryStream());
        Assert.Multiple(() =>
        {
            Assert.That(image.Height, Is.EqualTo(1));
            Assert.That(image.Width, Is.EqualTo(1));
        });
    }
}