using System.IO;
using System.Text.Json;
using ImageSpider.Core.Options;
using Microsoft.Extensions.Options;

namespace ImageSpider.App.Services;

public sealed class UserSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _userSettingsPath;
    private readonly IOptionsMonitor<SpiderOptions> _optionsMonitor;

    public UserSettingsStore(IOptionsMonitor<SpiderOptions> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor;
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ImageSpider");
        Directory.CreateDirectory(dir);
        _userSettingsPath = Path.Combine(dir, "appsettings.user.json");
    }

    public string UserSettingsPath => _userSettingsPath;

    public SpiderOptions LoadMerged()
    {
        var current = _optionsMonitor.CurrentValue;
        if (!File.Exists(_userSettingsPath))
            return current;

        try
        {
            var json = File.ReadAllText(_userSettingsPath);
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty(SpiderOptions.SectionName, out var spider))
                return current;

            var user = JsonSerializer.Deserialize<SpiderOptions>(spider.GetRawText(), JsonOptions);
            return user is null ? current : Merge(current, user);
        }
        catch
        {
            return current;
        }
    }

    public void Save(SpiderOptions options)
    {
        var wrapper = new Dictionary<string, object> { [SpiderOptions.SectionName] = options };
        File.WriteAllText(_userSettingsPath, JsonSerializer.Serialize(wrapper, JsonOptions));
    }

    private static SpiderOptions Merge(SpiderOptions b, SpiderOptions u)
    {
        b.Bing.Enabled = u.Bing.Enabled;
        if (!string.IsNullOrWhiteSpace(u.Bing.SubscriptionKey))
            b.Bing.SubscriptionKey = u.Bing.SubscriptionKey;

        b.Google.Enabled = u.Google.Enabled;
        if (!string.IsNullOrWhiteSpace(u.Google.ApiKey)) b.Google.ApiKey = u.Google.ApiKey;
        if (!string.IsNullOrWhiteSpace(u.Google.SearchEngineId)) b.Google.SearchEngineId = u.Google.SearchEngineId;

        b.Pexels.Enabled = u.Pexels.Enabled;
        if (!string.IsNullOrWhiteSpace(u.Pexels.ApiKey)) b.Pexels.ApiKey = u.Pexels.ApiKey;

        b.Pixabay.Enabled = u.Pixabay.Enabled;
        if (!string.IsNullOrWhiteSpace(u.Pixabay.ApiKey)) b.Pixabay.ApiKey = u.Pixabay.ApiKey;

        b.Unsplash.Enabled = u.Unsplash.Enabled;
        if (!string.IsNullOrWhiteSpace(u.Unsplash.AccessKey)) b.Unsplash.AccessKey = u.Unsplash.AccessKey;

        b.Scraper.BaiduEnabled = u.Scraper.BaiduEnabled;
        b.Scraper.SogouEnabled = u.Scraper.SogouEnabled;
        b.Scraper.So360Enabled = u.Scraper.So360Enabled;
        b.Scraper.BingScrapeEnabled = u.Scraper.BingScrapeEnabled;
        b.Scraper.DuckDuckGoEnabled = u.Scraper.DuckDuckGoEnabled;
        if (u.Scraper.RequestDelayMs > 0) b.Scraper.RequestDelayMs = u.Scraper.RequestDelayMs;

        if (!string.IsNullOrWhiteSpace(u.Download.DefaultDirectory))
            b.Download.DefaultDirectory = u.Download.DefaultDirectory;

        return b;
    }
}
