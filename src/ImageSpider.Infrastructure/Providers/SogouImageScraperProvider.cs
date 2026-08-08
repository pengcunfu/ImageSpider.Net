using System.Text.Json;
using System.Text.RegularExpressions;
using ImageSpider.Core.Abstractions;
using ImageSpider.Core.Models;
using ImageSpider.Core.Options;
using ImageSpider.Infrastructure.Http;
using Microsoft.Extensions.Options;

namespace ImageSpider.Infrastructure.Providers;

public sealed partial class SogouImageScraperProvider : IImageSearchProvider
{
    private readonly SpiderHttpClientFactory _clientFactory;
    private readonly IOptionsMonitor<SpiderOptions> _optionsMonitor;

    public SogouImageScraperProvider(SpiderHttpClientFactory clientFactory, IOptionsMonitor<SpiderOptions> optionsMonitor)
    {
        _clientFactory = clientFactory;
        _optionsMonitor = optionsMonitor;
    }

    private ScraperOptions _options => _optionsMonitor.CurrentValue.Scraper;

    public ImageSourceKind Source => ImageSourceKind.SogouScrape;
    public string DisplayName => "搜狗图片";
    public bool IsConfigured => _options.SogouEnabled;

    public async Task<ImageSearchResponse> SearchAsync(ImageSearchRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return new ImageSearchResponse { Items = [], HasMore = false };
        }

        var start = request.Page * request.PageSize;
        var query = Uri.EscapeDataString(request.Query.Trim());
        var url = $"https://pic.sogou.com/napi/pc/searchList?mode=1&start={start}&xml_len={request.PageSize}&query={query}";

        using var client = _clientFactory.CreateScraperClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://pic.sogou.com/");

        using var response = await client.GetAsync(url, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new ImageSearchResponse
            {
                Items = [],
                HasMore = false,
                Errors = [$"搜狗图片爬取失败 ({(int)response.StatusCode})"]
            };
        }

        var items = ParseSogouJson(body, request.Query);
        return new ImageSearchResponse
        {
            Items = items,
            HasMore = items.Count >= request.PageSize
        };
    }

    private static List<ImageResultItem> ParseSogouJson(string json, string query)
    {
        var results = new List<ImageResultItem>();

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("data", out var data) || !data.TryGetProperty("items", out var items))
                return results;

            foreach (var node in items.EnumerateArray())
            {
                var thumb = GetString(node, "picUrl") ?? GetString(node, "thumbUrl");
                var content = GetString(node, "oriPicUrl") ?? thumb;
                if (string.IsNullOrWhiteSpace(content))
                    continue;

                results.Add(new ImageResultItem
                {
                    Id = $"sogou-{GetString(node, "id") ?? Guid.NewGuid().ToString("N")}",
                    Title = GetString(node, "title") ?? query,
                    ThumbnailUrl = thumb ?? content,
                    ContentUrl = content,
                    HostPageUrl = GetString(node, "link"),
                    Width = GetInt(node, "width"),
                    Height = GetInt(node, "height"),
                    Source = ImageSourceKind.SogouScrape,
                    SourceDisplayName = "搜狗图片"
                });
            }
        }
        catch (JsonException)
        {
            foreach (Match m in PicUrlRegex().Matches(json))
            {
                var url = m.Groups[1].Value.Replace("\\/", "/");
                results.Add(new ImageResultItem
                {
                    Id = $"sogou-fb-{url.GetHashCode()}",
                    Title = query,
                    ThumbnailUrl = url,
                    ContentUrl = url,
                    Source = ImageSourceKind.SogouScrape,
                    SourceDisplayName = "搜狗图片"
                });
            }
        }

        return results;
    }

    private static string? GetString(JsonElement el, string name) =>
        el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

    private static int? GetInt(JsonElement el, string name) =>
        el.TryGetProperty(name, out var p) && p.TryGetInt32(out var v) ? v : null;

    [GeneratedRegex(@"""picUrl""\s*:\s*""([^""]+)""")]
    private static partial Regex PicUrlRegex();
}
