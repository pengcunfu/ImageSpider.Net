using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using ImageSpider.Core.Options;

namespace ImageSpider.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty] private string _bingSubscriptionKey = string.Empty;
    [ObservableProperty] private bool _bingEnabled = true;

    [ObservableProperty] private bool _googleEnabled;
    [ObservableProperty] private string _googleApiKey = string.Empty;
    [ObservableProperty] private string _googleSearchEngineId = string.Empty;

    [ObservableProperty] private bool _pexelsEnabled;
    [ObservableProperty] private string _pexelsApiKey = string.Empty;

    [ObservableProperty] private bool _pixabayEnabled;
    [ObservableProperty] private string _pixabayApiKey = string.Empty;

    [ObservableProperty] private bool _unsplashEnabled;
    [ObservableProperty] private string _unsplashAccessKey = string.Empty;

    [ObservableProperty] private bool _baiduEnabled = true;
    [ObservableProperty] private bool _sogouEnabled;
    [ObservableProperty] private bool _so360Enabled = true;
    [ObservableProperty] private bool _bingScrapeEnabled;
    [ObservableProperty] private bool _duckDuckGoEnabled;

    [ObservableProperty] private int _requestDelayMs = 300;
    [ObservableProperty] private string _downloadDirectory = string.Empty;

    public void LoadFrom(SpiderOptions options)
    {
        BingSubscriptionKey = options.Bing.SubscriptionKey;
        BingEnabled = options.Bing.Enabled;

        GoogleEnabled = options.Google.Enabled;
        GoogleApiKey = options.Google.ApiKey;
        GoogleSearchEngineId = options.Google.SearchEngineId;

        PexelsEnabled = options.Pexels.Enabled;
        PexelsApiKey = options.Pexels.ApiKey;

        PixabayEnabled = options.Pixabay.Enabled;
        PixabayApiKey = options.Pixabay.ApiKey;

        UnsplashEnabled = options.Unsplash.Enabled;
        UnsplashAccessKey = options.Unsplash.AccessKey;

        BaiduEnabled = options.Scraper.BaiduEnabled;
        SogouEnabled = options.Scraper.SogouEnabled;
        So360Enabled = options.Scraper.So360Enabled;
        BingScrapeEnabled = options.Scraper.BingScrapeEnabled;
        DuckDuckGoEnabled = options.Scraper.DuckDuckGoEnabled;
        RequestDelayMs = options.Scraper.RequestDelayMs;

        DownloadDirectory = string.IsNullOrWhiteSpace(options.Download.DefaultDirectory)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "ImageSpider")
            : options.Download.DefaultDirectory;
    }

    public SpiderOptions ToOptions() => new()
    {
        Bing = new BingOptions { Enabled = BingEnabled, SubscriptionKey = BingSubscriptionKey.Trim() },
        Google = new GoogleOptions
        {
            Enabled = GoogleEnabled,
            ApiKey = GoogleApiKey.Trim(),
            SearchEngineId = GoogleSearchEngineId.Trim()
        },
        Pexels = new PexelsOptions { Enabled = PexelsEnabled, ApiKey = PexelsApiKey.Trim() },
        Pixabay = new PixabayOptions { Enabled = PixabayEnabled, ApiKey = PixabayApiKey.Trim() },
        Unsplash = new UnsplashOptions { Enabled = UnsplashEnabled, AccessKey = UnsplashAccessKey.Trim() },
        Scraper = new ScraperOptions
        {
            BaiduEnabled = BaiduEnabled,
            SogouEnabled = SogouEnabled,
            So360Enabled = So360Enabled,
            BingScrapeEnabled = BingScrapeEnabled,
            DuckDuckGoEnabled = DuckDuckGoEnabled,
            RequestDelayMs = Math.Max(0, RequestDelayMs)
        },
        Download = new DownloadOptions { DefaultDirectory = DownloadDirectory.Trim() }
    };
}
