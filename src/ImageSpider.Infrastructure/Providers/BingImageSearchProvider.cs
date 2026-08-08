using System.Text.Json;
using ImageSpider.Core.Abstractions;
using ImageSpider.Core.Models;
using ImageSpider.Core.Options;
using Microsoft.Extensions.Options;

namespace ImageSpider.Infrastructure.Providers;

public sealed class BingImageSearchProvider : IImageSearchProvider
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<SpiderOptions> _optionsMonitor;

    public BingImageSearchProvider(HttpClient httpClient, IOptionsMonitor<SpiderOptions> optionsMonitor)
    {
        _httpClient = httpClient;
        _optionsMonitor = optionsMonitor;
    }

    private BingOptions _options => _optionsMonitor.CurrentValue.Bing;

    public ImageSourceKind Source => ImageSourceKind.BingApi;
    public string DisplayName => "Bing API";
    public bool IsConfigured => _options.Enabled && !string.IsNullOrWhiteSpace(_options.SubscriptionKey);

    public async Task<ImageSearchResponse> SearchAsync(ImageSearchRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return new ImageSearchResponse
            {
                Items = [],
                HasMore = false,
                Errors = ["Bing API 未配置：请在设置中填写 Subscription Key。"]
            };
        }

        var offset = request.Page * request.PageSize;
        var query = Uri.EscapeDataString(request.Query.Trim());
        var url = $"{_options.Endpoint.TrimEnd('/')}?q={query}&count={request.PageSize}&offset={offset}&safeSearch={(request.SafeSearch ? "Strict" : "Off")}&imageType={MapImageType(request.Type)}&size={MapImageSize(request.Size)}";

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
        httpRequest.Headers.Add("Ocp-Apim-Subscription-Key", _options.SubscriptionKey);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new ImageSearchResponse
            {
                Items = [],
                HasMore = false,
                Errors = [$"Bing API 请求失败 ({(int)response.StatusCode}): {Truncate(body, 200)}"]
            };
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var items = new List<ImageResultItem>();

        if (root.TryGetProperty("value", out var valueArray))
        {
            foreach (var node in valueArray.EnumerateArray())
            {
                var contentUrl = GetString(node, "contentUrl");
                var thumbUrl = GetString(node, "thumbnailUrl") ?? contentUrl;
                if (string.IsNullOrWhiteSpace(contentUrl))
                    continue;

                items.Add(new ImageResultItem
                {
                    Id = $"bing-{GetString(node, "imageId") ?? Guid.NewGuid().ToString("N")}",
                    Title = GetString(node, "name") ?? request.Query,
                    ThumbnailUrl = thumbUrl!,
                    ContentUrl = contentUrl,
                    HostPageUrl = GetString(node, "hostPageUrl"),
                    Width = GetInt(node, "width"),
                    Height = GetInt(node, "height"),
                    Source = ImageSourceKind.BingApi,
                    SourceDisplayName = DisplayName
                });
            }
        }

        var total = root.TryGetProperty("totalEstimatedMatches", out var totalNode)
            ? totalNode.GetInt64()
            : items.Count;
        var hasMore = offset + items.Count < total;

        return new ImageSearchResponse { Items = items, HasMore = hasMore };
    }

    private static string MapImageSize(ImageSizeFilter size) => size switch
    {
        ImageSizeFilter.Small => "Small",
        ImageSizeFilter.Medium => "Medium",
        ImageSizeFilter.Large => "Large",
        ImageSizeFilter.Wallpaper => "Wallpaper",
        _ => "All"
    };

    private static string MapImageType(ImageTypeFilter type) => type switch
    {
        ImageTypeFilter.Photo => "Photo",
        ImageTypeFilter.ClipArt => "Clipart",
        ImageTypeFilter.Line => "Line",
        ImageTypeFilter.Animated => "AnimatedGif",
        ImageTypeFilter.Transparent => "Transparent",
        _ => "All"
    };

    private static string? GetString(JsonElement el, string name) =>
        el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

    private static int? GetInt(JsonElement el, string name) =>
        el.TryGetProperty(name, out var p) && p.TryGetInt32(out var v) ? v : null;

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "...";
}
