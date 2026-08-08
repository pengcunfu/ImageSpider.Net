using System.Text.Json;
using ImageSpider.Core.Abstractions;
using ImageSpider.Core.Models;
using ImageSpider.Core.Options;
using ImageSpider.Infrastructure.Json;
using Microsoft.Extensions.Options;

namespace ImageSpider.Infrastructure.Providers;

public sealed class UnsplashImageSearchProvider : IImageSearchProvider
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<SpiderOptions> _optionsMonitor;

    public UnsplashImageSearchProvider(HttpClient httpClient, IOptionsMonitor<SpiderOptions> optionsMonitor)
    {
        _httpClient = httpClient;
        _optionsMonitor = optionsMonitor;
    }

    private UnsplashOptions _options => _optionsMonitor.CurrentValue.Unsplash;

    public ImageSourceKind Source => ImageSourceKind.UnsplashApi;
    public string DisplayName => "Unsplash";
    public bool IsConfigured => _options.Enabled && !string.IsNullOrWhiteSpace(_options.AccessKey);

    public async Task<ImageSearchResponse> SearchAsync(ImageSearchRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return new ImageSearchResponse
            {
                Items = [],
                HasMore = false,
                Errors = ["Unsplash 未配置：请在设置中填写 Access Key。"]
            };
        }

        var page = request.Page + 1;
        var query = Uri.EscapeDataString(request.Query.Trim());
        var url = $"https://api.unsplash.com/search/photos?query={query}&per_page={request.PageSize}&page={page}";

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
        httpRequest.Headers.TryAddWithoutValidation("Authorization", $"Client-ID {_options.AccessKey}");

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new ImageSearchResponse
            {
                Items = [],
                HasMore = false,
                Errors = [$"Unsplash API 失败 ({(int)response.StatusCode})"]
            };
        }

        var items = new List<ImageResultItem>();
        using var doc = JsonDocument.Parse(body);

        if (doc.RootElement.TryGetProperty("results", out var results))
        {
            foreach (var node in results.EnumerateArray())
            {
                var urls = node.TryGetProperty("urls", out var u) ? u : default;
                var content = urls.ValueKind != JsonValueKind.Undefined
                    ? JsonElementHelper.GetString(urls, "regular") ?? JsonElementHelper.GetString(urls, "full")
                    : null;
                var thumb = urls.ValueKind != JsonValueKind.Undefined
                    ? JsonElementHelper.GetString(urls, "small") ?? content
                    : null;

                if (string.IsNullOrWhiteSpace(content))
                    continue;

                items.Add(new ImageResultItem
                {
                    Id = $"unsplash-{JsonElementHelper.GetString(node, "id") ?? Guid.NewGuid().ToString("N")}",
                    Title = JsonElementHelper.GetString(node, "description")
                        ?? JsonElementHelper.GetString(node, "alt_description")
                        ?? request.Query,
                    ThumbnailUrl = thumb ?? content,
                    ContentUrl = content,
                    HostPageUrl = node.TryGetProperty("links", out var links)
                        ? JsonElementHelper.GetString(links, "html")
                        : null,
                    Width = JsonElementHelper.GetInt(node, "width"),
                    Height = JsonElementHelper.GetInt(node, "height"),
                    Source = ImageSourceKind.UnsplashApi,
                    SourceDisplayName = DisplayName
                });
            }
        }

        var total = JsonElementHelper.GetInt(doc.RootElement, "total") ?? 0;
        var hasMore = page * request.PageSize < total;

        return new ImageSearchResponse { Items = items, HasMore = hasMore };
    }
}
