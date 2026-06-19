namespace TownSuite.Web.ImageGen;

public class Settings
{
    public string CacheFolder { get; init; }
    public int CacheBackgroundCleanupTimerSeconds { get; init; }
    public int CacheMaxLifeTimeMinutes { get; init; }
    public int CacheSizeLimitInMiB { get; init; }
    public int HttpCacheControlMaxAgeInMinutes { get; init; }
    public int MaxWidth { get; init; }
    public int MaxHeight { get; init; }
    public string UserAgent { get; init; }

    // Image-proxy resource limits (DoS / open-proxy protection).
    // Maximum size of a remote response the proxy will download, in MiB.
    public int MaxDownloadSizeInMiB { get; init; }
    // Timeout for a single proxied download, in seconds.
    public int ProxyTimeoutSeconds { get; init; }
    // Maximum number of pixels (width * height) allowed in a *source* image before
    // decoding. Guards against decompression/pixel bombs that decode to huge buffers.
    public long MaxSourceImagePixels { get; init; }
}