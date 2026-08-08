using System.Text.Json;
using System.Text.RegularExpressions;
using ImageSpider.Core.Abstractions;
using ImageSpider.Core.Models;
using ImageSpider.Core.Options;
using ImageSpider.Infrastructure.Http;
using ImageSpider.Infrastructure.Json;
using Microsoft.Extensions.Options;

namespace ImageSpider.Infrastructure.Providers;

public sealed partial class DuckDuckGoImageScraperProvider : IImageSearchProvider
{
    private readonly SpiderHttpClientFactory _clientFactory;
    private readonly IOptionsMonitor<SpiderOptions> _optionsMonitor;

    public DuckDuckGoImageScraperProvider(SpiderHttpClientFactory clientFactory, IOptionsMonitor<SpiderOptions> optionsMonitor)
    {
        _clientFactory = clientFactory;
        _optionsMonitor = optionsMonitor;
    }

    private ScraperOptions _options => _optionsMonitor.CurrentValue.Scraper;

    public ImageSourceKind Source => ImageSourceKind.DuckDuckGoScrape;
    public string DisplayName => "DuckDuckGo";
    public bool IsConfigured => _options.DuckDuckGoEnabled;

    public async Task<ImageSearchResponse> SearchAsync(ImageSearchRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return new ImageSearchResponse { Items = [], HasMore = false };

        var query = request.Query.Trim();
        var vqd = await GetVqdAsync(query, cancellationToken);
        if (string.IsNullOrWhiteSpace(vqd))
        {
            return new ImageSearchResponse
            {
                Items = [],
                HasMore = false,
                Errors = ["DuckDuckGo：无法获取搜索令牌，请稍后重试。"]
            };
        }

        var page = request.Page + 1;
        var encoded = Uri.EscapeDataString(query);
        var url = $"https://duckduckgo.com/i.js?o=json&q={encoded}&vqd={vqd}&p={page}";

        using var client = _clientFactory.CreateScraperClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://duckduckgo.com/");

        using var response = await client.GetAsync(url, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new ImageSearchResponse
            {
                Items = [],
                HasMore = false,
                Errors = [$"DuckDuckGo 爬取失败 ({(int)response.StatusCode})"]
            };
        }

        var items = ParseResults(body, query);
        return new ImageSearchResponse
        {
            Items = items,
            HasMore = items.Count >= request.PageSize
        };
    }

    private async Task<string?> GetVqdAsync(string query, CancellationToken cancellationToken)
    {
        var encoded = Uri.EscapeDataString(query);
        var url = $"https://duckduckgo.com/?q={encoded}&iax=images&ia=images";

        using var client = _clientFactory.CreateScraperClient();
        using var response = await client.GetAsync(url, cancellationToken);
        var html = await response.Content.ReadAsStringAsync(cancellationToken);

        var match = VqdRegex().Match(html);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static List<ImageResultItem> ParseResults(string json, string query)
    {
        var results = new List<ImageResultItem>();

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("results", out var arr))
                return results;

            foreach (var node in arr.EnumerateArray())
            {
                var content = JsonElementHelper.GetString(node, "image");
                if (string.IsNullOrWhiteSpace(content))
                    continue;

                results.Add(new ImageResultItem
                {
                    Id = $"ddg-{content.GetHashCode()}",
                    Title = JsonElementHelper.GetString(node, "title") ?? query,
                    ThumbnailUrl = JsonElementHelper.GetString(node, "thumbnail") ?? content,
                    ContentUrl = content,
                    HostPageUrl = JsonElementHelper.GetString(node, "url"),
                    Width = JsonElementHelper.GetInt(node, "width"),
                    Height = JsonElementHelper.GetInt(node, "height"),
                    Source = ImageSourceKind.DuckDuckGoScrape,
                    SourceDisplayName = "DuckDuckGo"
                });
            }
        }
        catch (JsonException)
        {
            // ignore
        }

        return results;
    }

    [GeneratedRegex(@"vqd=([^&'""]+)")]
    private static partial Regex VqdRegex();
}
