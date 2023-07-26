using SixLabors.ImageSharp;

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
}