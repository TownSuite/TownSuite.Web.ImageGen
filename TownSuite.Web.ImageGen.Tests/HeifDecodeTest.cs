
namespace TownSuite.Web.ImageGen.Tests;

[TestFixture]
public class HeifDecodeTest
{
    [TestCase("avif" ,1204, 800),
     TestCase("heic", 700, 476)]
    public async Task CanConvertHeifToSharp(string imageformat, int width, int height)
    {
        var image = HeifDecoder.ConvertHeifToSharp(new MemoryStream(await File.ReadAllBytesAsync($"assets/{imageformat}_test.{imageformat}")));

        Assert.That(image.Height, Is.EqualTo(height));
        Assert.That(image.Width, Is.EqualTo(width));
    }
}