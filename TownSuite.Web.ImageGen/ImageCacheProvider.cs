using System.Net.Mime;
using SixLabors.ImageSharp;

namespace TownSuite.Web.ImageGen;

public class ImageCacheProvider : IImageCacheProvider
{
    
    private readonly Settings _settings;

    public ImageCacheProvider(Settings settings)
    {
        _settings = settings;
    }
    
    public async Task<ImageMetaData?> GetAsync(RequestMetaData rMetaData)
    {
        var file = new System.IO.FileInfo(rMetaData.Path);
        if (!file.Exists) return null;
        
        await using var fs = file.OpenRead();
        var format= await Image.DetectFormatAsync(fs);
        fs.Seek(0, SeekOrigin.Begin);
        return new ImageMetaData(file.LastWriteTimeUtc, TimeSpan.FromMinutes(_settings.HttpCacheControlMaxAgeInMinutes),
            fs.Length, file.Name, format.DefaultMimeType, rMetaData.Path);
    }

    public async Task Save(byte[] image, RequestMetaData rMetaData)
    {
        if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(rMetaData.Path)))
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(rMetaData.Path));
        }
        await System.IO.File.WriteAllBytesAsync(rMetaData.Path, image);   
    }
}