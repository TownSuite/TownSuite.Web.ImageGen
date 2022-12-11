using System.Net.Mime;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Web;
using Microsoft.Extensions.Primitives;

namespace TownSuite.Web.ImageGen;

public class ImageProxyRequestMetaData : RequestMetaData
{
    public string ImageSrcUrl { get; private set; }

    public bool WidthChangeRequested => base.widthChangeRequested;
    public bool HeightChangeRequested => base.heightChangeRequested;

    public virtual RequestMetaData GetRequestMetaData(Settings config, IQueryCollection query, string rawQueryString,
        string uriPath,
        string cacheSubFolder)
    {
        base.GetRequestMetaData(config, query, rawQueryString, uriPath, cacheSubFolder);
        ImageSrcUrl = HttpUtility.UrlDecode(PathParts.LastOrDefault());

        return this;
    }
}