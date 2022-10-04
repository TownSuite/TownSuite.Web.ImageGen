using SixLabors.ImageSharp.Web.Providers;
using SixLabors.ImageSharp.Web.Resolvers;

namespace TownSuite.Web.Avatars;

public class CustomImageProvider : IImageProvider
{
    
    // https://bartwullems.blogspot.com/2022/03/imagesharpwebcreate-your-own-image.html
    
    private readonly IImageRepository _imageRepository;

    /// <summary>
    /// A match function used by the resolver to identify itself as the correct resolver to use.
    /// </summary>
    private Func<HttpContext, bool> _match;

    public CustomImageProvider(IImageRepository repository)
    {
        _imageRepository = repository;
    }

    public ProcessingBehavior ProcessingBehavior { get; } = ProcessingBehavior.All;
        
    public Func<HttpContext, bool> Match
    {
        get => _match ?? IsMatch;
        set => _match = value;
    }

    public async Task<IImageResolver> GetAsync(HttpContext context)
    {
        //Extract image name from the querystring e.g. /image?id=<imagei>
        if (context.Request.Query.TryGetValue("id", out var value))
        { 
            var id=value.ToString();
            var (image, metadata)= await _imageRepository.Get(id);
                
            return new CustomImageResolver(image, metadata);
        }
        return null;
    }            

    public bool IsValidRequest(HttpContext context)=> true;

    private bool IsMatch(HttpContext context)
    {
        return context.Request.Path.Value.Contains("image");
    }
}