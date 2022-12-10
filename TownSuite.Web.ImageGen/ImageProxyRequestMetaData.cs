using System.Net.Mime;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Web;
using Microsoft.Extensions.Primitives;

namespace TownSuite.Web.ImageGen;

public class ImageProxyRequestMetaData : RequestMetaData
{
    public string ImageSrcUrl { get; private set; }
    

    public virtual RequestMetaData GetRequestMetaData(IConfiguration config, HttpContext ctx, string folder)
    {
        base.GetRequestMetaData(config, ctx, folder);
        StringValues proxy_src_image = string.Empty;
        ImageSrcUrl = HttpUtility.UrlDecode(PathParts.LastOrDefault());

        return this;
    }
}