
using LibHeifSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.InteropServices;

namespace TownSuite.Web.ImageGen.Tests;

[TestFixture]
public class HeifEncodeTest
{
    const int allowedPixelUncertainty = 1; // Encoded image RGB values may differ from original by up to this amount (0-255)

    [TestCase("color-no-alpha.png"),
     TestCase("grayscale-no-alpha.png"),
     TestCase("grayscale-alpha.png"),
     TestCase("color-alpha.png"),
     TestCase("facility.jpg")]
    public async Task CanConvertSharpToHeif(string imageName)
    {
        var origImage = await Image.LoadAsync($"assets/{imageName}");
        var width = origImage.Width;
        var height = origImage.Height;
        using HeifImage heifImage = HeifEncoder.ConvertSharpToHeif(origImage.CloneAs<Rgba32>());
        Assert.Multiple(() =>
        {
            Assert.That(heifImage.Height, Is.EqualTo(height), $"Photo {imageName} height incorrect");
            Assert.That(heifImage.Width, Is.EqualTo(width), $"Photo {imageName} width incorrect");
        });
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
        Assert.Multiple(() =>
        {
            Assert.That(isGrayscale, Is.EqualTo(expectGrayscale), $" Photo {imageName} {(expectGrayscale ? "Expects" : "Doesn't expect")} Grayscale");
            Assert.That(hasTransparency, Is.EqualTo(expectTransparency), $" Photo {imageName} {(expectTransparency ? "Expects" : "Doesn't expect")} Transparency");
        });
    }

    [TestCase("color-no-alpha.png"),
     TestCase("color-alpha.png")]
    public void ColorPixelsArentModifed(string imageName)
    {
        Image<Rgba32> origImage = Image.Load($"assets/{imageName}").CloneAs<Rgba32>();

        using var context = new HeifContext();
        using HeifImage heifImage = HeifEncoder.ConvertSharpToHeif(origImage);
        using LibHeifSharp.HeifEncoder encoder = context.GetEncoder(HeifCompressionFormat.Hevc);
        encoder.SetLossyQuality(85);
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
        Assert.Multiple(() =>
        {
            Assert.That(r, Is.InRange(origR - allowedPixelUncertainty, origR + allowedPixelUncertainty));
            Assert.That(g, Is.InRange(origG - allowedPixelUncertainty, origG + allowedPixelUncertainty));
            Assert.That(b, Is.InRange(origB - allowedPixelUncertainty, origB + allowedPixelUncertainty));
        });
    }

    [TestCase("grayscale-no-alpha.png"),
    TestCase("grayscale-alpha.png")]
    public void GrayscalePixelsArentModifed(string imageName)
    {
        Image<Rgba32> origImage = Image.Load($"assets/{imageName}").CloneAs<Rgba32>();

        using var context = new HeifContext();
        using HeifImage heifImage = HeifEncoder.ConvertSharpToHeif(origImage);
        using LibHeifSharp.HeifEncoder encoder = context.GetEncoder(HeifCompressionFormat.Hevc);
        encoder.SetLossyQuality(85);
        context.EncodeImage(heifImage, encoder);

        var origPixel = origImage[0, 0];
        var origR = origPixel.R;

        HeifPlaneData grayPlane = heifImage.GetPlane(HeifChannel.Y);
        IntPtr grayStartPtr = grayPlane.Scan0;

        var r = Marshal.ReadByte(grayStartPtr);

        Assert.That(r, Is.InRange(origR - allowedPixelUncertainty, origR + allowedPixelUncertainty));
    }
}
