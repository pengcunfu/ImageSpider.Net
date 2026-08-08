using ImageSpider.Core.Options;
using Microsoft.Extensions.Options;

namespace ImageSpider.Infrastructure.Http;

public sealed class SpiderHttpClientFactory
{
    private readonly IOptionsMonitor<SpiderOptions> _optionsMonitor;

    public SpiderHttpClientFactory(IOptionsMonitor<SpiderOptions> optionsMonitor) =>
        _optionsMonitor = optionsMonitor;

    private ScraperOptions _options => _optionsMonitor.CurrentValue.Scraper;

    public HttpClient CreateScraperClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", _options.UserAgent);
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html,application/json,*/*");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");
        return client;
    }
}
