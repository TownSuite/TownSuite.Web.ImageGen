using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace TownSuite.Web.ImageGen.Tests;

[TestFixture]
public class HeifDecodeTest
{
    [TestCase("avif", 200, 200),
     TestCase("heic", 200, 200)]
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

    private static bool HeifAvailable()
    {
        // HeifDecoder.Available() throws (rather than returning false) when the native
        // libheif library can't be loaded, so guard it for machines without the codec.
        try { return HeifDecoder.Available(); }
        catch { return false; }
    }

    [TestCase("avif"), TestCase("heic")]
    public async Task ConvertHeifToSharp_ThrowsWhenSourceExceedsMaxPixels(string imageformat)
    {
        Assume.That(HeifAvailable(), "libheif decoder not available on this machine");

        var imageName = $"assets/{imageformat}_test.{imageformat}";
        using var ms = new MemoryStream(await File.ReadAllBytesAsync(imageName));

        // The test assets are 200x200 = 40,000 px; a cap of 1 must be rejected before decode.
        Assert.Throws<InvalidOperationException>(() => HeifDecoder.ConvertHeifToSharp(ms, maxPixels: 1));
    }
}