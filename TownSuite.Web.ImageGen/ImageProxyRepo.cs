using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace TownSuite.Web.ImageGen;

public class ImageProxyRepo : IImageRepository
{
    public string Folder { get; } = "imageproxy";
    private readonly IImageDownloader _downloader;

    public ImageProxyRepo(IImageDownloader downloader)
    {
        _downloader = downloader;
    }

    public async Task<(byte[] imageData, ImageMetaData metadata)> Get(RequestMetaData request)
    {
        var proxyRequest = request as ImageProxyRequestMetaData;
        var img = await _downloader.Download(proxyRequest.ImageSrcUrl);

        if (proxyRequest.WidthChangeRequested && proxyRequest.HeightChangeRequested)
        {
            img.Mutate(x => x
                .Resize(proxyRequest.Width, proxyRequest.Height));
        }
        // If you pass 0 as any of the values for width and height dimensions then 
        // ImageSharp will automatically determine the correct opposite dimensions 
        // size to preserve the original aspect ratio.
        else if (proxyRequest.WidthChangeRequested)
        {
            img.Mutate(x => x
                .Resize(proxyRequest.Width, 0));
        }
        else if (proxyRequest.HeightChangeRequested)
        {
            img.Mutate(x => x
                .Resize(0, proxyRequest.Height));
        }
        
        var img2 = await Helper.BinaryAsBytes(img, proxyRequest.ImageFormat);
        var md = new ImageMetaData(DateTime.UtcNow, TimeSpan.FromDays(360), img2.image.Length,
            $"{request.Id}.{img2.fileExt}",
            img2.contentType);
        return (img2.image, md);
    }

    
}