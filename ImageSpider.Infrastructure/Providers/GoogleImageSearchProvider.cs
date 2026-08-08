using System.Text.Json;
using ImageSpider.Core.Abstractions;
using ImageSpider.Core.Models;
using ImageSpider.Core.Options;
using ImageSpider.Infrastructure.Json;
using Microsoft.Extensions.Options;

namespace ImageSpider.Infrastructure.Providers;

public sealed class GoogleImageSearchProvider : IImageSearchProvider
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<SpiderOptions> _optionsMonitor;

    public GoogleImageSearchProvider(HttpClient httpClient, IOptionsMonitor<SpiderOptions> optionsMonitor)
    {
        _httpClient = httpClient;
        _optionsMonitor = optionsMonitor;
    }

    private GoogleOptions _options => _optionsMonitor.CurrentValue.Google;

    public ImageSourceKind Source => ImageSourceKind.GoogleApi;
    public string DisplayName => "Google 图片";
    public bool IsConfigured => _options.Enabled
        && !string.IsNullOrWhiteSpace(_options.ApiKey)
        && !string.IsNullOrWhiteSpace(_options.SearchEngineId);

    public async Task<ImageSearchResponse> SearchAsync(ImageSearchRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return new ImageSearchResponse
            {
                Items = [],
                HasMore = false,
                Errors = ["Google 未配置：请在设置中填写 API Key 与搜索引擎 ID (cx)。"]
            };
        }

        var start = request.Page * request.PageSize + 1;
        var query = Uri.EscapeDataString(request.Query.Trim());
        var safe = request.SafeSearch ? "active" : "off";
        var url = $"https://www.googleapis.com/customsearch/v1?q={query}&searchType=image&key={_options.ApiKey}&cx={_options.SearchEngineId}&start={start}&num={request.PageSize}&safe={safe}";

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new ImageSearchResponse
            {
                Items = [],
                HasMore = false,
                Errors = [$"Google API 失败 ({(int)response.StatusCode}): {Truncate(body, 180)}"]
            };
        }

        var items = new List<ImageResultItem>();
        using var doc = JsonDocument.Parse(body);

        if (doc.RootElement.TryGetProperty("items", out var arr))
        {
            foreach (var node in arr.EnumerateArray())
            {
                var link = JsonElementHelper.GetString(node, "link");
                if (string.IsNullOrWhiteSpace(link))
                    continue;

                var image = node.TryGetProperty("image", out var img) ? img : default;
                items.Add(new ImageResultItem
                {
                    Id = $"google-{Guid.NewGuid():N}",
                    Title = JsonElementHelper.GetString(node, "title") ?? request.Query,
                    ThumbnailUrl = image.ValueKind != JsonValueKind.Undefined
                        ? JsonElementHelper.GetString(image, "thumbnailLink") ?? link
                        : link,
                    ContentUrl = link,
                    HostPageUrl = image.ValueKind != JsonValueKind.Undefined
                        ? JsonElementHelper.GetString(image, "contextLink")
                        : null,
                    Width = image.ValueKind != JsonValueKind.Undefined ? JsonElementHelper.GetInt(image, "width") : null,
                    Height = image.ValueKind != JsonValueKind.Undefined ? JsonElementHelper.GetInt(image, "height") : null,
                    Source = ImageSourceKind.GoogleApi,
                    SourceDisplayName = DisplayName
                });
            }
        }

        var hasMore = doc.RootElement.TryGetProperty("queries", out var queries)
            && queries.TryGetProperty("nextPage", out var next)
            && next.GetArrayLength() > 0;

        return new ImageSearchResponse { Items = items, HasMore = hasMore || items.Count >= request.PageSize };
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "...";
}
