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
    private readonly Settings _settings;

    public ImageProxyRepo(IImageDownloader downloader, Settings settings)
    {
        _downloader = downloader;
        _settings = settings;
    }

    public async Task<(byte[] imageData, ImageMetaData metadata)> Get(RequestMetaData request)
    {
        var proxyRequest = request as ImageProxyRequestMetaData;
        var result = await _downloader.Download(proxyRequest.ImageSrcUrl);
        await using var downloadStream = result.S;
        if (string.Equals(result.ContentType, "image/svg+xml", StringComparison.InvariantCultureIgnoreCase))
        {
            using var ms = new MemoryStream();
            await downloadStream.CopyToAsync(ms);
            var svg = ms.ToArray();
            var mdSvg = new ImageMetaData(DateTime.UtcNow, TimeSpan.FromMinutes(_settings.HttpCacheControlMaxAgeInMinutes), svg.Length,
                $"{request.Id}.svg",
                "image/svg+xml", request.Path);
            return (svg, mdSvg);
        }
        
        Image img;

        if (string.Equals(result.ContentType, "image/avif", StringComparison.InvariantCultureIgnoreCase)
                       || string.Equals(result.ContentType, "image/heic", StringComparison.InvariantCultureIgnoreCase))
        {
            img = HeifDecoder.ConvertHeifToSharp(downloadStream);
        }
        else
        {
            img = await Image.LoadAsync(downloadStream);
        }

        if (proxyRequest.WidthChangeRequested && proxyRequest.HeightChangeRequested
            && ResizeRequestIsSmallerOrEqualToOrignalSize(proxyRequest, img))
        {
            img.Mutate(x => x
                .Resize(proxyRequest.Width, proxyRequest.Height));
        }
        // If you pass 0 as any of the values for width and height dimensions then 
        // ImageSharp will automatically determine the correct opposite dimensions 
        // size to preserve the original aspect ratio.
        else if (proxyRequest.WidthChangeRequested && ResizeRequestIsSmallerOrEqualToOrignalSize(proxyRequest, img))
        {
            img.Mutate(x => x
                .Resize(proxyRequest.Width, 0));
        }
        else if (proxyRequest.HeightChangeRequested && ResizeRequestIsSmallerOrEqualToOrignalSize(proxyRequest, img))
        {
            img.Mutate(x => x
                .Resize(0, proxyRequest.Height));
        }
        
        var img2 = await Helper.BinaryAsBytes(img, proxyRequest.ImageFormat);
        var md = new ImageMetaData(DateTime.UtcNow, TimeSpan.FromMinutes(_settings.HttpCacheControlMaxAgeInMinutes), img2.image.Length,
            $"{request.Id}.{img2.fileExt}",
            img2.contentType, request.Path);
        return (img2.image, md);
    }

    /// <summary>
    /// If an resize request is larger than the original image size we will just return the original size.
    /// Do not make the image larger as it will become blurry and distorted.
    /// </summary>
    /// <returns></returns>
    private bool ResizeRequestIsSmallerOrEqualToOrignalSize(ImageProxyRequestMetaData proxyRequest, Image img)
    {
        return proxyRequest.Height <= img.Height && proxyRequest.Width <= img.Width;
    }
    
}