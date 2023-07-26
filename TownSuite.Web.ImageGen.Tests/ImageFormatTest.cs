
namespace TownSuite.Web.ImageGen.Tests;

[TestFixture]
public class ImageFormatTest
{
    [TestCase("avif" ,ImageFormat.Format.avif),
     TestCase("image/jpg", ImageFormat.Format.jpeg)]
    public void ReturnsExpectedFormat(string given, ImageFormat.Format expected)
    {
        ImageFormat.Format returnedFormat = ImageFormat.GetFormat(given);
        Assert.That(returnedFormat, Is.EqualTo(expected), $"Expected {expected} but got {returnedFormat}");
    }

    private static readonly string[] cases = new string[] {" ", "", "test", "image/", "AjpgB"};
    [TestCaseSource(nameof(cases))]
    public void ReturnsPngIfInvalidType(string given)
    {
        ImageFormat.Format returnedFormat = ImageFormat.GetFormat(given);
        Console.WriteLine(returnedFormat.GetType());
        
        Assert.That(returnedFormat, Is.EqualTo(ImageFormat.Format.png), $"Expected {ImageFormat.Format.png} but got {returnedFormat}");
    }
    
}
