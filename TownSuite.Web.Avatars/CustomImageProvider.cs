using SixLabors.ImageSharp.Web.Providers;
using SixLabors.ImageSharp.Web.Resolvers;

namespace TownSuite.Web.Avatars;

public class CustomImageProvider : IImageProvider
{
    // https://bartwullems.blogspot.com/2022/03/imagesharpwebcreate-your-own-image.html
    private readonly IImageRepository _imageRepository;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// A match function used by the resolver to identify itself as the correct resolver to use.
    /// </summary>
    private Func<HttpContext, bool> _match;

    public CustomImageProvider(IImageRepository repository, IConfiguration configuration)
    {
        _imageRepository = repository;
        _configuration = configuration;
    }

    public ProcessingBehavior ProcessingBehavior { get; } = ProcessingBehavior.All;

    public Func<HttpContext, bool> Match
    {
        get => _match ?? IsMatch;
        set => _match = value;
    }

    public async Task<IImageResolver> GetAsync(HttpContext context)
    {
        // /avatar/676540b1672d727fa2f02d62965120d7?d=identicon&s=100%22
        // Extract image name from the querystring e.g. /avatar/someid?otheroptions=123
        // being someid as id
        string id = context.Request.Path.Value.Split("/").LastOrDefault();
        var (image, metadata) = await _imageRepository.Get(id);

        return new CustomImageResolver(image, metadata);
    }

    public bool IsValidRequest(HttpContext context) => true;

    private bool IsMatch(HttpContext context)
    {
        string pathMatch = _configuration.GetValue<string>("PathMatch");
        return context.Request.Path.Value.Contains(pathMatch);
    }
}