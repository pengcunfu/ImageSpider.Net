namespace ImageSpider.Core.Models;

public sealed class ImageResultItem
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string ThumbnailUrl { get; init; }
    public required string ContentUrl { get; init; }
    public string? HostPageUrl { get; init; }
    public int? Width { get; init; }
    public int? Height { get; init; }
    public required ImageSourceKind Source { get; init; }
    public string? SourceDisplayName { get; init; }
}
