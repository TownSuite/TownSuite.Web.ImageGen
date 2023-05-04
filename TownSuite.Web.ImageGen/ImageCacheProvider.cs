using System.Net.Mime;
using System.Text;
using SixLabors.ImageSharp;

namespace TownSuite.Web.ImageGen;

public class ImageCacheProvider : IImageCacheProvider
{

    private readonly Settings _settings;

    public ImageCacheProvider(Settings settings)
    {
        _settings = settings;
    }

    async Task<ImageMetaData?> IImageCacheProvider.GetAsync(RequestMetaData rMetaData)
    {
        var file = new System.IO.FileInfo(rMetaData.Path);
        if (!file.Exists) return null;

        await using var fs = file.OpenRead();
        var format = await SixLabors.ImageSharp.Image.DetectFormatAsync(fs);
        string mimetype = format?.DefaultMimeType;
        if (string.IsNullOrWhiteSpace(mimetype))
        {
          if (IsSvg(fs))
          {
           mimetype = "image/svg+xml";
          }
          else
          {
           mimetype = "";
          }
        }
        fs.Seek(0, SeekOrigin.Begin);
        return new ImageMetaData(file.LastWriteTimeUtc, TimeSpan.FromMinutes(_settings.HttpCacheControlMaxAgeInMinutes),
            fs.Length, file.Name, mimetype, rMetaData.Path);
    }

    async Task IImageCacheProvider.Save(byte[] image, RequestMetaData rMetaData)
    {
        if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(rMetaData.Path)))
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(rMetaData.Path));
        }
        await System.IO.File.WriteAllBytesAsync(rMetaData.Path, image);
    }
    public bool IsSvg(Stream stream)
    {
        int headerSize = (int)Math.Min(512, stream.Length);
        if (headerSize <= 0)
        {
            return false;
        }
        var headersBuffer = new byte[headerSize];
        long startPosition = stream.Position;

        int n = 0;
        int i;
        do
        {
            i = stream.Read(headersBuffer, n, headerSize - n);
            n += i;
        }
        while (n < headerSize && i > 0);

        stream.Position = startPosition;

        string header = Encoding.UTF8.GetString(headersBuffer);
        string svg1 = Encoding.UTF8.GetString(HexToBytes("3C73766720"));
        string svg2 = Encoding.UTF8.GetString(HexToBytes("3C21444F43545950452073766720"));
        string svg3 = Encoding.UTF8.GetString(HexToBytes("3C3F786D6C20"));

        return header.Contains(svg1) || header.Contains(svg2) || header.Contains(svg3);
    }

    byte[] HexToBytes(string hex)
    {
        var bytes = new byte[hex.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }
        return bytes;
    }
}