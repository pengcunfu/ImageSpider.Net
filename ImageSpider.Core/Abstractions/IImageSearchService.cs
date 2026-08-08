using ImageSpider.Core.Models;

namespace ImageSpider.Core.Abstractions;

public interface IImageSearchService
{
    Task<ImageSearchResponse> SearchAsync(ImageSearchRequest request, CancellationToken cancellationToken = default);
    IReadOnlyList<IImageSearchProvider> GetProviders();
}
