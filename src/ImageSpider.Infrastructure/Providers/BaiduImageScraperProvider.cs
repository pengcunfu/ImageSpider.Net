using System.Text.Json;
using System.Text.RegularExpressions;
using ImageSpider.Core.Abstractions;
using ImageSpider.Core.Models;
using ImageSpider.Core.Options;
using ImageSpider.Infrastructure.Baidu;
using ImageSpider.Infrastructure.Http;
using Microsoft.Extensions.Options;

namespace ImageSpider.Infrastructure.Providers;

public sealed partial class BaiduImageScraperProvider : IImageSearchProvider
{
    private readonly SpiderHttpClientFactory _clientFactory;
    private readonly IOptionsMonitor<SpiderOptions> _optionsMonitor;
    private static readonly SemaphoreSlim RateLimit = new(1, 1);
    private DateTime _lastRequestUtc = DateTime.MinValue;

    public BaiduImageScraperProvider(SpiderHttpClientFactory clientFactory, IOptionsMonitor<SpiderOptions> optionsMonitor)
    {
        _clientFactory = clientFactory;
        _optionsMonitor = optionsMonitor;
    }

    private ScraperOptions _options => _optionsMonitor.CurrentValue.Scraper;

    public ImageSourceKind Source => ImageSourceKind.BaiduScrape;
    public string DisplayName => "百度图片";
    public bool IsConfigured => _options.BaiduEnabled;

    public async Task<ImageSearchResponse> SearchAsync(ImageSearchRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return new ImageSearchResponse { Items = [], HasMore = false };
        }

        await ApplyRateLimitAsync(cancellationToken);

        var pn = request.Page * request.PageSize;
        var query = Uri.EscapeDataString(request.Query.Trim());
        var url = $"https://image.baidu.com/search/acjson?tn=resultjson_com&ipn=rj&ct=201326592&fp=result&queryWord={query}&word={query}&pn={pn}&rn={request.PageSize}&ie=utf-8&oe=utf-8";

        using var client = _clientFactory.CreateScraperClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://image.baidu.com/");

        using var response = await client.GetAsync(url, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new ImageSearchResponse
            {
                Items = [],
                HasMore = false,
                Errors = [$"百度图片爬取失败 ({(int)response.StatusCode})"]
            };
        }

        body = SanitizeJson(body);
        var items = ParseBaiduJson(body, request.Query);

        return new ImageSearchResponse
        {
            Items = items,
            HasMore = items.Count >= request.PageSize
        };
    }

    private async Task ApplyRateLimitAsync(CancellationToken cancellationToken)
    {
        await RateLimit.WaitAsync(cancellationToken);
        try
        {
            var delay = _options.RequestDelayMs - (int)(DateTime.UtcNow - _lastRequestUtc).TotalMilliseconds;
            if (delay > 0)
                await Task.Delay(delay, cancellationToken);
            _lastRequestUtc = DateTime.UtcNow;
        }
        finally
        {
            RateLimit.Release();
        }
    }

    private static string SanitizeJson(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "{}";

        var start = raw.IndexOf('{');
        if (start > 0)
            raw = raw[start..];

        return InvalidEscapeRegex().Replace(raw, m =>
        {
            var hex = m.Groups[1].Value;
            if (hex.Length == 0) return m.Value;
            try
            {
                var code = Convert.ToInt32(hex, 16);
                return char.IsControl((char)code) ? " " : m.Value;
            }
            catch
            {
                return " ";
            }
        });
    }

    private static List<ImageResultItem> ParseBaiduJson(string json, string query)
    {
        var results = new List<ImageResultItem>();

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
                return results;

            foreach (var node in data.EnumerateArray())
            {
                if (node.ValueKind != JsonValueKind.Object)
                    continue;

                var thumb = ResolveUrl(FirstNonEmpty(node, "thumbURL", "middleURL", "hoverURL"));
                var content = ResolveUrl(
                    FirstNonEmpty(node, "objURL"),
                    FirstNonEmpty(node, "middleURL", "replaceUrl", "hoverURL", "thumbURL"));
                if (string.IsNullOrWhiteSpace(content))
                    continue;

                thumb = thumb ?? content;

                var title = FirstNonEmpty(node, "fromPageTitleEnc", "fromPageTitle", "title") ?? query;
                title = Uri.UnescapeDataString(title.Replace("+", " "));

                var hostPage = ResolveUrl(FirstNonEmpty(node, "fromURL", "page_url"));

                results.Add(new ImageResultItem
                {
                    Id = $"baidu-{GetHashCode(content)}",
                    Title = title,
                    ThumbnailUrl = thumb,
                    ContentUrl = content,
                    HostPageUrl = hostPage,
                    Width = GetIntProperty(node, "width"),
                    Height = GetIntProperty(node, "height"),
                    Source = ImageSourceKind.BaiduScrape,
                    SourceDisplayName = "百度图片"
                });
            }
        }
        catch (JsonException)
        {
            results.AddRange(ParseBaiduFallbackRegex(json, query));
        }

        return results;
    }

    private static List<ImageResultItem> ParseBaiduFallbackRegex(string text, string query)
    {
        var list = new List<ImageResultItem>();
        var thumbMatches = ThumbUrlRegex().Matches(text);
        var objMatches = ObjUrlRegex().Matches(text);
        var count = Math.Min(thumbMatches.Count, objMatches.Count);

        for (var i = 0; i < count; i++)
        {
            var content = ResolveUrl(objMatches[i].Groups[1].Value) ?? "";
            var thumb = ResolveUrl(thumbMatches[i].Groups[1].Value) ?? content;
            if (string.IsNullOrWhiteSpace(content))
                continue;

            list.Add(new ImageResultItem
            {
                Id = $"baidu-fb-{GetHashCode(content)}",
                Title = query,
                ThumbnailUrl = thumb,
                ContentUrl = content,
                Source = ImageSourceKind.BaiduScrape,
                SourceDisplayName = "百度图片"
            });
        }

        return list;
    }

    private static string? FirstNonEmpty(JsonElement el, params string[] names)
    {
        foreach (var name in names)
        {
            if (!el.TryGetProperty(name, out var p))
                continue;
            var s = p.ValueKind == JsonValueKind.String ? p.GetString() : p.ToString();
            if (!string.IsNullOrWhiteSpace(s))
                return s;
        }
        return null;
    }

    private static int? GetIntProperty(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p))
            return null;
        if (p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out var n))
            return n;
        if (p.ValueKind == JsonValueKind.String && int.TryParse(p.GetString(), out var parsed))
            return parsed;
        return null;
    }

    /// <summary>优先返回可访问的 http(s) 地址：先尝试解密 objURL，否则用 CDN 直链。</summary>
    private static string? ResolveUrl(params string?[] candidates)
    {
        string? fallback = null;

        foreach (var raw in candidates)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var decoded = BaiduUrlDecoder.Decode(raw);
            if (BaiduUrlDecoder.IsHttpUrl(decoded))
                return decoded;

            if (BaiduUrlDecoder.IsHttpUrl(raw))
                return raw.Trim();

            fallback ??= decoded ?? raw.Trim();
        }

        return fallback;
    }

    private static int GetHashCode(string s) => StringComparer.OrdinalIgnoreCase.GetHashCode(s);

    [GeneratedRegex(@"\\x([0-9a-fA-F]{2})")]
    private static partial Regex InvalidEscapeRegex();

    [GeneratedRegex(@"""thumbURL""\s*:\s*""([^""\\]*(?:\\.[^""\\]*)*)""")]
    private static partial Regex ThumbUrlRegex();

    [GeneratedRegex(@"""objURL""\s*:\s*""([^""\\]*(?:\\.[^""\\]*)*)""")]
    private static partial Regex ObjUrlRegex();
}
