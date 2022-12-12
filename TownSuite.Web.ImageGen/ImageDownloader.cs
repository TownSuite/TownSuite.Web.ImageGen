using SixLabors.ImageSharp;

namespace TownSuite.Web.ImageGen;

public class ImageDownloader : IImageDownloader
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly Settings _settings;

    public ImageDownloader(IHttpClientFactory clientFactory, Settings settings)
    {
        _clientFactory = clientFactory;
        _settings = settings;
    }

    public async Task<Image> Download(string srcUrl)
    {
        var client = _clientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, srcUrl);
        request.Headers.Add("User-Agent", _settings.UserAgent);
        
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await Image.LoadAsync(await response.Content.ReadAsStreamAsync());
    }
}