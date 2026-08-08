using ImageSpider.Core.Abstractions;
using ImageSpider.Core.Models;

namespace ImageSpider.Infrastructure.Services;

public sealed class ImageSearchService : IImageSearchService
{
    private readonly IReadOnlyList<IImageSearchProvider> _providers;

    public ImageSearchService(IEnumerable<IImageSearchProvider> providers) =>
        _providers = providers.ToList();

    public IReadOnlyList<IImageSearchProvider> GetProviders() => _providers;

    public async Task<ImageSearchResponse> SearchAsync(ImageSearchRequest request, CancellationToken cancellationToken = default)
    {
        var sources = request.Sources.Count > 0
            ? request.Sources.ToHashSet()
            : _providers.Select(p => p.Source).ToHashSet();

        var active = _providers.Where(p => sources.Contains(p.Source)).ToList();
        if (active.Count == 0)
        {
            return new ImageSearchResponse
            {
                Items = [],
                HasMore = false,
                Errors = ["请至少选择一个搜索来源。"]
            };
        }

        var tasks = active.Select(p => SearchSafeAsync(p, request, cancellationToken)).ToArray();
        var responses = await Task.WhenAll(tasks);

        var errors = responses.SelectMany(r => r.Errors).Distinct().ToList();
        var merged = responses
            .SelectMany(r => r.Items)
            .GroupBy(i => i.ContentUrl, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        var hasMore = responses.Any(r => r.HasMore);

        return new ImageSearchResponse
        {
            Items = merged,
            HasMore = hasMore,
            Errors = errors
        };
    }

    private static async Task<ImageSearchResponse> SearchSafeAsync(
        IImageSearchProvider provider,
        ImageSearchRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await provider.SearchAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            return new ImageSearchResponse
            {
                Items = [],
                HasMore = false,
                Errors = [$"{provider.DisplayName}: {ex.Message}"]
            };
        }
    }
}
