namespace ImageSpider.Core.Models;

public sealed class ImageSearchResponse
{
    public required IReadOnlyList<ImageResultItem> Items { get; init; }
    public bool HasMore { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}
