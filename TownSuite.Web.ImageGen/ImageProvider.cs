using System.Security.Cryptography;
using Microsoft.Extensions.Primitives;

namespace TownSuite.Web.ImageGen;

public class ImageProvider
{
    private readonly IImageRepository _imageRepository;
    private readonly IImageCacheProvider _cache;

    public ImageProvider(IImageRepository repository, IImageCacheProvider cache)
    {
        _imageRepository = repository;
        _cache = cache;
    }

    public async Task<ImageMetaData> GetAsync(RequestMetaData rMetaData)
    {
        var cacheResults = await _cache.GetAsync(rMetaData);
        if (cacheResults != null)
        {
            return cacheResults;
        }

        var results = await _imageRepository.Get(rMetaData);
        await _cache.Save(results.imageData, rMetaData);
        return results.metadata;
    }
}