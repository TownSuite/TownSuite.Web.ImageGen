namespace TownSuite.Web.ImageGen;

public class Settings
{
    public string CacheFolder { get; init; }
    public int CacheBackgroundCleanupTimerSeconds { get; init; }
    public int CacheMaxLifeTimeMinutes { get; init; }
    public int CacheSizeLimitInMiB { get; init; }
    public int MaxWidth { get; init; }
    public int MaxHeight { get; init; }
    public string UserAgent { get; init; }
}