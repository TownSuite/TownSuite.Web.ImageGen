using SixLabors.ImageSharp;

namespace TownSuite.Web.ImageGen;

public class ImageDownloader : IImageDownloader
{
    private readonly IHttpClientFactory _clientFactory;

    public ImageDownloader(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }
    
    public async Task<Image> Download(string srcUrl)
    {
        var client = _clientFactory.CreateClient();
        using var response = await client.GetAsync(srcUrl);
        response.EnsureSuccessStatusCode();
        return await Image.LoadAsync(await response.Content.ReadAsStreamAsync());
    }
}