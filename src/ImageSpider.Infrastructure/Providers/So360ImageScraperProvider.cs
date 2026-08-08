using System.Text.Json;
using ImageSpider.Core.Abstractions;
using ImageSpider.Core.Models;
using ImageSpider.Core.Options;
using ImageSpider.Infrastructure.Http;
using ImageSpider.Infrastructure.Json;
using Microsoft.Extensions.Options;

namespace ImageSpider.Infrastructure.Providers;

public sealed class So360ImageScraperProvider : IImageSearchProvider
{
    private readonly SpiderHttpClientFactory _clientFactory;
    private readonly IOptionsMonitor<SpiderOptions> _optionsMonitor;

    public So360ImageScraperProvider(SpiderHttpClientFactory clientFactory, IOptionsMonitor<SpiderOptions> optionsMonitor)
    {
        _clientFactory = clientFactory;
        _optionsMonitor = optionsMonitor;
    }

    private ScraperOptions _options => _optionsMonitor.CurrentValue.Scraper;

    public ImageSourceKind Source => ImageSourceKind.So360Scrape;
    public string DisplayName => "360图片";
    public bool IsConfigured => _options.So360Enabled;

    public async Task<ImageSearchResponse> SearchAsync(ImageSearchRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return new ImageSearchResponse { Items = [], HasMore = false };

        var pn = request.Page * request.PageSize;
        var query = Uri.EscapeDataString(request.Query.Trim());
        var url = $"https://image.so.com/j?q={query}&pn={pn}&sn=0";

        using var client = _clientFactory.CreateScraperClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://image.so.com/");

        using var response = await client.GetAsync(url, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new ImageSearchResponse
            {
                Items = [],
                HasMore = false,
                Errors = [$"360图片爬取失败 ({(int)response.StatusCode})"]
            };
        }

        var items = ParseJson(body, request.Query);
        return new ImageSearchResponse
        {
            Items = items,
            HasMore = items.Count >= request.PageSize
        };
    }

    private static List<ImageResultItem> ParseJson(string json, string query)
    {
        var results = new List<ImageResultItem>();

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("list", out var list))
                return results;

            foreach (var node in list.EnumerateArray())
            {
                var content = JsonElementHelper.GetString(node, "img") ?? JsonElementHelper.GetString(node, "imgurl");
                var thumb = JsonElementHelper.GetString(node, "thumb") ?? JsonElementHelper.GetString(node, "thumb_bak") ?? content;
                if (string.IsNullOrWhiteSpace(content))
                    continue;

                results.Add(new ImageResultItem
                {
                    Id = $"360-{content.GetHashCode()}",
                    Title = JsonElementHelper.GetString(node, "title") ?? query,
                    ThumbnailUrl = thumb!,
                    ContentUrl = content,
                    HostPageUrl = JsonElementHelper.GetString(node, "link"),
                    Width = JsonElementHelper.GetInt(node, "width"),
                    Height = JsonElementHelper.GetInt(node, "height"),
                    Source = ImageSourceKind.So360Scrape,
                    SourceDisplayName = "360图片"
                });
            }
        }
        catch (JsonException)
        {
            // ignore
        }

        return results;
    }
}
