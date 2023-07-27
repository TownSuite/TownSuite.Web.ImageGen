
namespace TownSuite.Web.ImageGen.Tests;

[TestFixture]
public class ImageFormatTest
{
    [TestCase("png",       ImageFormat.Format.png),
     TestCase("image/jpg", ImageFormat.Format.jpeg),
     TestCase("svg+xml",   ImageFormat.Format.svg),
     TestCase("   heif ",  ImageFormat.Format.heic)]
    public void ReturnsExpectedFormat(string given, ImageFormat.Format expected)
    {
        ImageFormat.Format returnedFormat = ImageFormat.GetFormat(given);
        Assert.That(returnedFormat, Is.EqualTo(expected), $"Expected {expected} but got {returnedFormat}");
    }

    private static readonly string[] cases = new string[] { " ", "", "abxz", "image/", "123456" };
    [TestCaseSource(nameof(cases))]
    public void ReturnsPngOnInvalidType(string given)
    {
        ImageFormat.Format returnedFormat = ImageFormat.GetFormat(given);
        Assert.That(returnedFormat, Is.EqualTo(ImageFormat.Format.png), $"Expected {ImageFormat.Format.png} but got {returnedFormat}");
    }
}
