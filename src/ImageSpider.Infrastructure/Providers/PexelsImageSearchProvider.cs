using System.Text.Json;
using ImageSpider.Core.Abstractions;
using ImageSpider.Core.Models;
using ImageSpider.Core.Options;
using ImageSpider.Infrastructure.Json;
using Microsoft.Extensions.Options;

namespace ImageSpider.Infrastructure.Providers;

public sealed class PexelsImageSearchProvider : IImageSearchProvider
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<SpiderOptions> _optionsMonitor;

    public PexelsImageSearchProvider(HttpClient httpClient, IOptionsMonitor<SpiderOptions> optionsMonitor)
    {
        _httpClient = httpClient;
        _optionsMonitor = optionsMonitor;
    }

    private PexelsOptions _options => _optionsMonitor.CurrentValue.Pexels;

    public ImageSourceKind Source => ImageSourceKind.PexelsApi;
    public string DisplayName => "Pexels";
    public bool IsConfigured => _options.Enabled && !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<ImageSearchResponse> SearchAsync(ImageSearchRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return new ImageSearchResponse
            {
                Items = [],
                HasMore = false,
                Errors = ["Pexels 未配置：请在设置中填写 API Key。"]
            };
        }

        var page = request.Page + 1;
        var query = Uri.EscapeDataString(request.Query.Trim());
        var url = $"https://api.pexels.com/v1/search?query={query}&per_page={request.PageSize}&page={page}";

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
        httpRequest.Headers.TryAddWithoutValidation("Authorization", _options.ApiKey);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new ImageSearchResponse
            {
                Items = [],
                HasMore = false,
                Errors = [$"Pexels API 失败 ({(int)response.StatusCode})"]
            };
        }

        var items = new List<ImageResultItem>();
        using var doc = JsonDocument.Parse(body);

        if (doc.RootElement.TryGetProperty("photos", out var photos))
        {
            foreach (var node in photos.EnumerateArray())
            {
                var src = node.TryGetProperty("src", out var srcEl) ? srcEl : default;
                var content = src.ValueKind != JsonValueKind.Undefined
                    ? JsonElementHelper.GetString(src, "large2x") ?? JsonElementHelper.GetString(src, "large")
                    : null;
                var thumb = src.ValueKind != JsonValueKind.Undefined
                    ? JsonElementHelper.GetString(src, "medium") ?? content
                    : null;

                if (string.IsNullOrWhiteSpace(content))
                    continue;

                items.Add(new ImageResultItem
                {
                    Id = $"pexels-{JsonElementHelper.GetInt(node, "id") ?? 0}",
                    Title = JsonElementHelper.GetString(node, "alt") ?? request.Query,
                    ThumbnailUrl = thumb ?? content,
                    ContentUrl = content,
                    HostPageUrl = JsonElementHelper.GetString(node, "url"),
                    Width = JsonElementHelper.GetInt(node, "width"),
                    Height = JsonElementHelper.GetInt(node, "height"),
                    Source = ImageSourceKind.PexelsApi,
                    SourceDisplayName = DisplayName
                });
            }
        }

        var total = JsonElementHelper.GetInt(doc.RootElement, "total_results") ?? 0;
        var hasMore = page * request.PageSize < total;

        return new ImageSearchResponse { Items = items, HasMore = hasMore };
    }
}
