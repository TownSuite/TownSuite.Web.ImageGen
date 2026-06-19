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
        // Reject non-http(s) schemes before doing anything else. The connection-time
        // SSRF checks (private/loopback/link-local blocking, no redirects) are enforced
        // by the "imageproxy" named client configured in Program.cs.
        SsrfGuard.ValidateUrl(srcUrl);
        var client = _clientFactory.CreateClient("imageproxy");
        using var request = new HttpRequestMessage(HttpMethod.Get, srcUrl);
        request.Headers.Add("User-Agent", _settings.UserAgent);

        long maxBytes = (_settings.MaxDownloadSizeInMiB > 0 ? _settings.MaxDownloadSizeInMiB : 25) * 1024L * 1024L;

        // ResponseHeadersRead so we can reject oversized responses without buffering them.
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        // Reject early if the server advertises a size over the limit...
        if (response.Content.Headers.ContentLength is long advertised && advertised > maxBytes)
        {
            throw new InvalidOperationException("Remote image exceeds the maximum allowed download size.");
        }

        var ms = new MemoryStream();
        // ...and enforce the cap while streaming, in case Content-Length is absent or lies
        // (e.g. chunked transfer encoding).
        await using (var src = await response.Content.ReadAsStreamAsync())
        {
            await CopyWithLimitAsync(src, ms, maxBytes);
        }
        ms.Position = 0;
        return (ms, response.Content.Headers?.ContentType?.ToString() ?? "");
    }

    private static async Task CopyWithLimitAsync(Stream source, Stream destination, long maxBytes)
    {
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await source.ReadAsync(buffer)) > 0)
        {
            total += read;
            if (total > maxBytes)
            {
                throw new InvalidOperationException("Remote image exceeds the maximum allowed download size.");
            }

            await destination.WriteAsync(buffer.AsMemory(0, read));
        }
    }
}