using ImageSpider.Core.Models;

namespace ImageSpider.Infrastructure.Http;

public static class RefererHelper
{
    public static string? GetReferer(ImageSourceKind source) => source switch
    {
        ImageSourceKind.BaiduScrape => "https://image.baidu.com/",
        ImageSourceKind.SogouScrape => "https://pic.sogou.com/",
        ImageSourceKind.So360Scrape => "https://image.so.com/",
        ImageSourceKind.BingScrape => "https://www.bing.com/",
        ImageSourceKind.DuckDuckGoScrape => "https://duckduckgo.com/",
        _ => null
    };

    public static void ApplyReferer(HttpRequestMessage request, ImageSourceKind source)
    {
        var referer = GetReferer(source);
        if (referer is not null)
            request.Headers.TryAddWithoutValidation("Referer", referer);
    }

    public static void ApplyReferer(HttpClient client, ImageSourceKind source)
    {
        var referer = GetReferer(source);
        if (referer is not null)
            client.DefaultRequestHeaders.TryAddWithoutValidation("Referer", referer);
    }
}
