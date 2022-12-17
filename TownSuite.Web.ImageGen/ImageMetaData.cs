namespace TownSuite.Web.ImageGen;

public class ImageMetaData
{
    public ImageMetaData(DateTime lastModifiedUtc, TimeSpan expires, long contentLength,
        string filename, string contentType, string fullFilePath)
    {
        LastModifiedUtc = lastModifiedUtc;
        Expires = expires;
        ContentLength = contentLength;
        Filename = filename;
        ContentType = contentType;
        FullFilePath = fullFilePath;
    }
    public DateTime LastModifiedUtc { get; init; }
    public TimeSpan Expires { get; init; }
    public long ContentLength { get; init; }
    public string Filename { get; init; }
    public string ContentType { get; init; }
    public string FullFilePath { get; init; }

}