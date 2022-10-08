
using System.Security.Cryptography;

namespace TownSuite.Web.ImageGen;

public class ImageProvider 
{
    private readonly IImageRepository _imageRepository;
    private readonly IConfiguration _configuration;
    private readonly IImageCacheProvider _cache;
    /// <summary>
    /// A match function used by the resolver to identify itself as the correct resolver to use.
    /// </summary>
    private Func<HttpContext, bool> _match;
    
    public ImageProvider(IImageRepository repository, IConfiguration configuration, IImageCacheProvider cache)
    {
        _imageRepository = repository;
        _configuration = configuration;
        _cache = cache;
    }
    
    public async Task<(byte[] image, ImageMetaData metadata)> GetAsync(HttpContext context)
    {
        string id = Hash(context.Request.Path.Value.Split("/").LastOrDefault());
        string cacheFolder = _configuration.GetValue<string>("CacheFolder");
        string path = System.IO.Path.Combine(cacheFolder, _imageRepository.Folder, $"{id}.png");
        var cacheResults = await _cache.GetAsync(path);
        if (cacheResults.image != null)
        {
            return cacheResults;
        }
        
        var results = await _imageRepository.Get(id);
        await _cache.Save(results.imageData, path);
        return results;
    }
    static string Hash(string nonHashedString)
    {
        using var md5 = MD5.Create();
        byte[] data = System.Text.Encoding.UTF8.GetBytes(nonHashedString);
        byte[] retVal = md5.ComputeHash(data);
        return BitConverter.ToString(retVal);
    }
    
}