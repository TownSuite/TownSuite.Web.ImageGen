using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Web;
using SixLabors.ImageSharp.Web.Middleware;
using SixLabors.ImageSharp.Web.Resolvers;

namespace TownSuite.Web.Avatars;

internal class CustomImageResolver : IImageResolver
{
    private byte[] _data;
    private ImageMetadata _metadata;

    public CustomImageResolver(byte[] data, ImageMetadata metadata)
    {
        _data = data;
        _metadata = metadata;
    }

    public Task<ImageMetadata> GetMetaDataAsync()
    {
        return Task.FromResult(new ImageMetadata(_metadata.LastWriteTimeUtc, _data.Length)); 
    }

    public Task<Stream> OpenReadAsync()
    {
        return Task.FromResult<Stream>(new MemoryStream(_data));
    }
}