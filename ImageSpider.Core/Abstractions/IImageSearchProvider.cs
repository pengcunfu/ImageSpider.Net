using ImageSpider.Core.Models;

namespace ImageSpider.Core.Abstractions;

public interface IImageSearchProvider
{
    ImageSourceKind Source { get; }
    string DisplayName { get; }
    bool IsConfigured { get; }
    Task<ImageSearchResponse> SearchAsync(ImageSearchRequest request, CancellationToken cancellationToken = default);
}
