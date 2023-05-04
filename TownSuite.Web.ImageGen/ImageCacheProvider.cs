using System.Diagnostics.Eventing.Reader;
using System.Net.Mime;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using MimeDetective;
using MimeDetective.Definitions;
using MimeDetective.Storage;
using MimeDetective.Storage.Xml.v2;
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
           mimetype = Getmimetype(fs);
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
    string Getmimetype(FileStream fs)
    {
        var Inspector = new ContentInspectorBuilder()
        {
            Definitions = new MimeDetective.Definitions.CondensedBuilder()
            {
                UsageType = MimeDetective.Definitions.Licensing.UsageType.PersonalNonCommercial
            }.Build()
        }.Build();

        var Results = Inspector.Inspect(fs);
        var ResultsByMimeType = Results.ByMimeType();
        return Convert.ToString(ResultsByMimeType);
    }
}