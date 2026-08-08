namespace ImageSpider.Core.Models;

public sealed class ImageSearchRequest
{
    public required string Query { get; init; }
    public int Page { get; init; } = 0;
    public int PageSize { get; init; } = 30;
    public IReadOnlyList<ImageSourceKind> Sources { get; init; } = [];
    public ImageSizeFilter Size { get; init; } = ImageSizeFilter.All;
    public ImageTypeFilter Type { get; init; } = ImageTypeFilter.All;
    public bool SafeSearch { get; init; } = true;
}

public enum ImageSizeFilter
{
    All,
    Small,
    Medium,
    Large,
    Wallpaper
}

public enum ImageTypeFilter
{
    All,
    Photo,
    ClipArt,
    Line,
    Animated,
    Transparent
}
