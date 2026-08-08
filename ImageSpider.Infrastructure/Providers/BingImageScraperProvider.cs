using System.Text.RegularExpressions;
using ImageSpider.Core.Abstractions;
using ImageSpider.Core.Models;
using ImageSpider.Core.Options;
using ImageSpider.Infrastructure.Http;
using Microsoft.Extensions.Options;

namespace ImageSpider.Infrastructure.Providers;

public sealed partial class BingImageScraperProvider : IImageSearchProvider
{
    private readonly SpiderHttpClientFactory _clientFactory;
    private readonly IOptionsMonitor<SpiderOptions> _optionsMonitor;

    public BingImageScraperProvider(SpiderHttpClientFactory clientFactory, IOptionsMonitor<SpiderOptions> optionsMonitor)
    {
        _clientFactory = clientFactory;
        _optionsMonitor = optionsMonitor;
    }

    private ScraperOptions _options => _optionsMonitor.CurrentValue.Scraper;

    public ImageSourceKind Source => ImageSourceKind.BingScrape;
    public string DisplayName => "必应图片";
    public bool IsConfigured => _options.BingScrapeEnabled;

    public async Task<ImageSearchResponse> SearchAsync(ImageSearchRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return new ImageSearchResponse { Items = [], HasMore = false };

        var first = request.Page * request.PageSize;
        var query = Uri.EscapeDataString(request.Query.Trim());
        var safe = request.SafeSearch ? "strict" : "off";
        var url = $"https://www.bing.com/images/async?q={query}&first={first}&count={request.PageSize}&safeSearch={safe}&mmasync=1";

        using var client = _clientFactory.CreateScraperClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://www.bing.com/images/search");

        using var response = await client.GetAsync(url, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new ImageSearchResponse
            {
                Items = [],
                HasMore = false,
                Errors = [$"必应图片爬取失败 ({(int)response.StatusCode})"]
            };
        }

        var items = ParseHtml(body, request.Query);
        return new ImageSearchResponse
        {
            Items = items,
            HasMore = items.Count >= request.PageSize
        };
    }

    private static List<ImageResultItem> ParseHtml(string html, string query)
    {
        var results = new List<ImageResultItem>();
        var murls = MurlRegex().Matches(html);
        var turls = TurlRegex().Matches(html);
        var purls = PurlRegex().Matches(html);
        var count = murls.Count;

        for (var i = 0; i < count; i++)
        {
            var content = DecodeHtml(murls[i].Groups[1].Value);
            if (string.IsNullOrWhiteSpace(content) || !content.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                continue;

            var thumb = i < turls.Count ? DecodeHtml(turls[i].Groups[1].Value) : content;
            var page = i < purls.Count ? DecodeHtml(purls[i].Groups[1].Value) : null;

            results.Add(new ImageResultItem
            {
                Id = $"bing-scrape-{content.GetHashCode()}",
                Title = query,
                ThumbnailUrl = thumb,
                ContentUrl = content,
                HostPageUrl = page,
                Source = ImageSourceKind.BingScrape,
                SourceDisplayName = "必应图片"
            });
        }

        return results;
    }

    private static string DecodeHtml(string s) =>
        System.Net.WebUtility.HtmlDecode(s.Replace("\\u002f", "/").Replace("\\/", "/"));

    [GeneratedRegex(@"murl&quot;:&quot;([^&]+?)&quot;")]
    private static partial Regex MurlRegex();

    [GeneratedRegex(@"turl&quot;:&quot;([^&]+?)&quot;")]
    private static partial Regex TurlRegex();

    [GeneratedRegex(@"purl&quot;:&quot;([^&]+?)&quot;")]
    private static partial Regex PurlRegex();
}
