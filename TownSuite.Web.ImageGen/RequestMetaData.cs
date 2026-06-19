using System.Reflection.Metadata;
using System.Security.Cryptography;
using Microsoft.Extensions.Primitives;

namespace TownSuite.Web.ImageGen;

public class RequestMetaData
{
    public int Height { get; private set; }
    protected bool heightChangeRequested = false;
    public int Width { get; private set; }
    protected bool widthChangeRequested = false;
    public string Path { get; private set; }
    public string Id { get; private set; }
    public string ImageFormat { get; private set; }

    public string[] PathParts { get; private set; }

    public virtual RequestMetaData GetRequestMetaData(Settings config, IQueryCollection query, string rawQueryString,
        string uriPath,
        string cacheSubFolder)
    {
        PathParts = uriPath.Split("/");
        string id = Hash($"{PathParts.LastOrDefault()}{rawQueryString}");
        string cacheFolder = config.CacheFolder;
        StringValues image_format = string.Empty;

        int width = 80;
        int height = 80;
        int maxWidth = config.MaxWidth;
        int maxHeight = config.MaxHeight;
        // identicons
        query.TryGetValue("s", out var strSize);
        int.TryParse(strSize, out var size);

        if (query.ContainsKey("w"))
        {
            query.TryGetValue("w", out strSize);

            int.TryParse(strSize, out width);
            widthChangeRequested = true;
        }

        if (query.ContainsKey("h"))
        {
            query.TryGetValue("h", out strSize);
            int.TryParse(strSize, out height);
            heightChangeRequested = true;
        }


        if (query.ContainsKey("imgformat"))
        {
            query.TryGetValue("imgformat", out image_format);
        }
        else
        {
            image_format = "png";
        }

        // Normalize the requested format to a known, safe enum value BEFORE it is
        // ever used to build a filesystem path. The raw "imgformat" query value is
        // attacker-controlled and was previously interpolated straight into the cache
        // path, allowing path-traversal (e.g. imgformat=png/../../../../etc/passwd).
        // GetFormat returns one of the fixed ImageFormat.Format names (falling back to
        // png), so it can never contain '/', '\' or ".." separators.
        string safeFormat = global::TownSuite.Web.ImageGen.ImageFormat.GetFormat(image_format).ToString();

        string path;
        if (size == 0)
        {
            if (height > maxHeight || width > maxWidth)
            {
                throw new Exception($"Max allowable width is {maxWidth} and max allowable height is {maxHeight}");
            }

            path = BuildCachePath(cacheFolder, cacheSubFolder, $"{id}_{width}_{height}.{safeFormat}");

            this.Height = height;
            this.Width = width;
            this.Path = path;
            this.Id = id;
            this.ImageFormat = safeFormat;

            return this;
        }

        if (size > maxHeight || size > maxWidth)
        {
            throw new Exception($"Max allowable width is {maxWidth} and max allowable height is {maxHeight}");
        }

        path = BuildCachePath(cacheFolder, cacheSubFolder, $"{id}_{size}_{size}.{safeFormat}");

        this.Height = size;
        this.Width = size;
        this.Path = path;
        this.Id = id;
        this.ImageFormat = safeFormat;


        return this;
    }

    /// <summary>
    /// Combines the cache folder, sub-folder and file name, then verifies the resolved
    /// path stays inside the configured cache folder. Defense-in-depth against path
    /// traversal in any path segment (the file name is already restricted to a hash plus
    /// a normalized format, but this guards against future regressions and misconfig).
    /// </summary>
    static string BuildCachePath(string cacheFolder, string cacheSubFolder, string fileName)
    {
        string cacheRoot = System.IO.Path.GetFullPath(cacheFolder);
        string fullPath = System.IO.Path.GetFullPath(
            System.IO.Path.Combine(cacheRoot, cacheSubFolder, fileName));

        string prefix = cacheRoot.EndsWith(System.IO.Path.DirectorySeparatorChar)
            ? cacheRoot
            : cacheRoot + System.IO.Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(prefix, StringComparison.Ordinal))
        {
            throw new ArgumentException("Resolved cache path escapes the cache directory.");
        }

        return fullPath;
    }

    static string Hash(string nonHashedString)
    {
        // SHA-256 (not MD5): MD5 is collision-broken, which for a cache key means an
        // attacker could craft two distinct inputs that map to the same cache file and
        // poison/confuse cached content. This value is only a cache key/filename, so a
        // plain (non-salted) cryptographic hash is appropriate.
        byte[] data = System.Text.Encoding.UTF8.GetBytes(nonHashedString);
        byte[] retVal = SHA256.HashData(data);
        return Convert.ToHexString(retVal);
    }
}