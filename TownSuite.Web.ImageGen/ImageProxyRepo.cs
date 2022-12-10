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
    private readonly IHttpClientFactory _clientFactory;

    public ImageProxyRepo(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    public async Task<(byte[] imageData, ImageMetaData metadata)> Get(RequestMetaData request)
    {
        var x = request as ImageProxyRequestMetaData;

        using HttpClient client = new HttpClient();
        using var response = await client.GetAsync(x.ImageSrcUrl);
        response.EnsureSuccessStatusCode();
        var img = await Image.LoadAsync(await response.Content.ReadAsStreamAsync());
        var img2 = await Helper.BinaryAsBytes(img, x.ImageFormat);
        var md = new ImageMetaData(DateTime.UtcNow, TimeSpan.FromDays(360), img2.image.Length,
            $"{request.Id}.{img2.fileExt}",
            img2.contentType);
        return (img2.image, md);
    }

    
}