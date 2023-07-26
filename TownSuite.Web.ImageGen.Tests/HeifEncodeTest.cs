
using LibHeifSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.InteropServices;

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

        using HeifImage heifImage = HeifEncoder.ConvertSharpToHeif(origImage.CloneAs<Rgba32>());

        Assert.That(heifImage.Height, Is.EqualTo(height));
        Assert.That(heifImage.Width, Is.EqualTo(width));
    }

    [TestCase("color-no-alpha.png",     false, false),
     TestCase("grayscale-no-alpha.png", true,  false),
     TestCase("grayscale-alpha.png",    true,  true),
     TestCase("color-alpha.png",        false, true),
     TestCase("facility.jpg",           false, false)]
    public async Task AnalyzeGrayscaleAndTransparency(string imageName, bool expectGrayscale, bool expectTransparency)
    {
        var origImage = await Image.LoadAsync($"assets/{imageName}");
        (bool isGrayscale, bool hasTransparency) = HeifEncoder.AnalyzeImage(origImage.CloneAs<Rgba32>());
        Assert.That(isGrayscale, Is.EqualTo(expectGrayscale));
        Assert.That(hasTransparency, Is.EqualTo(expectTransparency));
    }

    [TestCase("color-no-alpha.png"),
     TestCase("color-alpha.png")]
    public async Task ColorPixelsArentModifed(string imageName)
    {
        Image<Rgba32> origImage = Image.Load($"assets/{imageName}").CloneAs<Rgba32>();

        using var context = new HeifContext();
        HeifImage heifImage = HeifEncoder.ConvertSharpToHeif(origImage);
        LibHeifSharp.HeifEncoder encoder = context.GetEncoder(HeifCompressionFormat.Hevc);
        encoder.SetLossyQuality(90); // Quality higher to avoid artifacts
        context.EncodeImage(heifImage, encoder);

        var origPixel = origImage[0, 0];
        var origR = origPixel.R;
        var origG = origPixel.G;
        var origB = origPixel.B;

        HeifPlaneData grayPlane = heifImage.GetPlane(HeifChannel.Interleaved);
        IntPtr grayStartPtr = grayPlane.Scan0;

        var r = Marshal.ReadByte(grayStartPtr);
        var g = Marshal.ReadByte(grayStartPtr + 1);
        var b = Marshal.ReadByte(grayStartPtr + 2);
       
        Assert.That(r, Is.InRange(origR - 1, origR + 1));
        Assert.That(g, Is.InRange(origG - 1, origG + 1));
        Assert.That(b, Is.InRange(origB - 1, origB + 1));
    }

    [TestCase("grayscale-no-alpha.png"),
    TestCase("grayscale-alpha.png")]
    public async Task GrayscalePixelsArentModifed(string imageName)
    {
        Image<Rgba32> origImage = Image.Load($"assets/{imageName}").CloneAs<Rgba32>();

        using var context = new HeifContext();
        HeifImage heifImage = HeifEncoder.ConvertSharpToHeif(origImage);
        LibHeifSharp.HeifEncoder encoder = context.GetEncoder(HeifCompressionFormat.Hevc);
        encoder.SetLossyQuality(90); // Quality higher to avoid artifacts
        context.EncodeImage(heifImage, encoder);

        var origPixel = origImage[0, 0];
        var origR = origPixel.R;

        HeifPlaneData grayPlane = heifImage.GetPlane(HeifChannel.Y);
        IntPtr grayStartPtr = grayPlane.Scan0;

        var r = Marshal.ReadByte(grayStartPtr);

        Assert.That(r, Is.InRange(origR - 1, origR + 1));
    }
}
