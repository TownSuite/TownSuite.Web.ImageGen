using System.Reflection.Metadata;

namespace TownSuite.Web.ImageGen;

public class RequestMetaData
{
    public RequestMetaData(int height, int width, string path, string id)
    {
        Height = height;
        Width = width;
        Path = path;
        Id = id;
    }

    public int Height { get; init; }
    public int Width { get; init; }
    public string Path { get; init; }
    public string Id { get; init; }
}