namespace TownSuite.Web.ImageGen;

public class ImageCacheProvider : IImageCacheProvider
{
    public async Task<(byte[] image, ImageMetaData metadata)> GetAsync(string path)
    {
        var file = new System.IO.FileInfo(path);
        if (file.Exists)
        {
            await using var stream = file.OpenRead();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var image = ms.ToArray();
            
            return (image, new ImageMetaData(file.LastWriteTimeUtc, TimeSpan.FromDays(300),
                image.Length, file.Name));
        }

        return (null, null);
    }

    public async Task Save(byte[] image, string path)
    {
        if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(path)))
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
        }
        await System.IO.File.WriteAllBytesAsync(path, image);   
    }
}