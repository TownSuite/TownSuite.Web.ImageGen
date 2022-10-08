
using System.Security.Cryptography;
using Microsoft.Extensions.Primitives;

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
    
    public async Task<(byte[] image, ImageMetaData metadata)> GetAsync(RequestMetaData rMetaData)
    {
        var cacheResults = await _cache.GetAsync(rMetaData);
        if (cacheResults.image != null)
        {
            return cacheResults;
        }
        var results = await _imageRepository.Get(rMetaData.Id, rMetaData.Width, rMetaData.Height);
        await _cache.Save(results.imageData, rMetaData);
        return results;
    }
    
}