namespace TownSuite.Web.ImageGen;

public class BackgroundWorkerService : BackgroundService
{
    private readonly Settings _settings;

    private readonly ILogger<BackgroundWorkerService> _logger;

    public BackgroundWorkerService(ILogger<BackgroundWorkerService> logger,
        Settings settings)
    {
        _logger = logger;
        _settings = settings;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(_settings.CacheBackgroundCleanupTimerSeconds), cancellationToken);
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Cache cleanup running");
                long cacheSizeInMiB = DirSizeInMiB(new DirectoryInfo(_settings.CacheFolder));
                if (cacheSizeInMiB < _settings.CacheSizeLimitInMiB)
                {
                    _logger.LogInformation("Cache cleanup.  Nothing to do.");
                    continue;
                }

                CleanCacheOfOldFiles(new DirectoryInfo(_settings.CacheFolder), _settings.CacheMaxLifeTimeMinutes);
                _logger.LogInformation("Cache cleanup ran");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "");
            }
            finally
            {
                await Task.Delay(TimeSpan.FromSeconds(_settings.CacheBackgroundCleanupTimerSeconds), cancellationToken);
            }

            
        }
    }


    static void CleanCacheOfOldFiles(DirectoryInfo d, int cacheMaxLifeTimeMinutes)
    {
        var startTime =  TimeSpan.FromTicks(DateTime.UtcNow.Ticks).TotalMinutes;
        foreach (var fi in d.EnumerateFiles())
        {
            var fileAge = TimeSpan.FromTicks(fi.CreationTimeUtc.Ticks).TotalMinutes;
            if ((startTime - fileAge) >
                cacheMaxLifeTimeMinutes)
            {
                fi.Delete();
            }
        }

        foreach (var di in d.EnumerateDirectories())
        {
            CleanCacheOfOldFiles(di, cacheMaxLifeTimeMinutes);
        }
    }


    static long DirSizeInMiB(DirectoryInfo d)
    {
        // https://en.wikipedia.org/wiki/Byte#Multiple-byte_units
        return DirSizeInBytes(d) / 1024 / 1024;
    }

    // See https://stackoverflow.com/questions/468119/whats-the-best-way-to-calculate-the-size-of-a-directory-in-net
    static long DirSizeInBytes(DirectoryInfo d)
    {
        long size = 0;

        foreach (var fi in d.EnumerateFiles())
        {
            size += fi.Length;
        }

        foreach (var di in d.EnumerateDirectories())
        {
            size += DirSizeInBytes(di);
        }

        return size;
    }
}