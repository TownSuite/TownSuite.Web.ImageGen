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

        string path;
        if (size == 0)
        {
            if (height > maxHeight || width > maxWidth)
            {
                throw new Exception($"Max allowable width is {maxWidth} and max allowable height is {maxHeight}");
            }

            path = System.IO.Path.Combine(cacheFolder, cacheSubFolder, $"{id}_{width}_{height}.{image_format}");

            this.Height = height;
            this.Width = width;
            this.Path = path;
            this.Id = id;
            this.ImageFormat = image_format;

            return this;
        }

        if (size > maxHeight || size > maxWidth)
        {
            throw new Exception($"Max allowable width is {maxWidth} and max allowable height is {maxHeight}");
        }

        path = System.IO.Path.Combine(cacheFolder, cacheSubFolder, $"{id}_{size}_{size}.{image_format}");

        this.Height = size;
        this.Width = size;
        this.Path = path;
        this.Id = id;
        this.ImageFormat = image_format;


        return this;
    }

    static string Hash(string nonHashedString)
    {
        using var md5 = MD5.Create();
        byte[] data = System.Text.Encoding.UTF8.GetBytes(nonHashedString);
        byte[] retVal = md5.ComputeHash(data);
        return BitConverter.ToString(retVal);
    }
}