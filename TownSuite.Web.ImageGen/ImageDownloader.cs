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

    public async Task<(Stream S, string ContentType)> Download(string srcUrl)
    {
        var client = _clientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, srcUrl);
        request.Headers.Add("User-Agent", _settings.UserAgent);
        
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var ms = new MemoryStream();
        await response.Content.CopyToAsync(ms);
        await ms.FlushAsync();
        ms.Position = 0;
        return (ms, response.Content.Headers?.ContentType?.ToString() ?? "");
    }
}