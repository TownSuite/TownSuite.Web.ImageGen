using System.Net.Mime;
using SixLabors.ImageSharp;

namespace TownSuite.Web.ImageGen;

public class ImageCacheProvider : IImageCacheProvider
{
    public async Task<(byte[] image, ImageMetaData metadata)> GetAsync(RequestMetaData rMetaData)
    {
        var file = new System.IO.FileInfo(rMetaData.Path);
        if (file.Exists)
        {
            await using var stream = file.OpenRead();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var image = ms.ToArray();

            ms.Seek(0, SeekOrigin.Begin);
            var format= await Image.DetectFormatAsync(ms);
            return (image, new ImageMetaData(file.LastWriteTimeUtc, TimeSpan.FromDays(300),
                image.Length, file.Name, format.DefaultMimeType));
        }

        return (null, null);
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