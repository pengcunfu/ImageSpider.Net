namespace ImageSpider.Core.Options;

public sealed class SpiderOptions
{
    public const string SectionName = "Spider";

    public BingOptions Bing { get; set; } = new();
    public GoogleOptions Google { get; set; } = new();
    public PexelsOptions Pexels { get; set; } = new();
    public PixabayOptions Pixabay { get; set; } = new();
    public UnsplashOptions Unsplash { get; set; } = new();
    public ScraperOptions Scraper { get; set; } = new();
    public DownloadOptions Download { get; set; } = new();
}

public sealed class BingOptions
{
    public string SubscriptionKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "https://api.bing.microsoft.com/v7.0/images/search";
    public bool Enabled { get; set; } = true;
}

public sealed class GoogleOptions
{
    public bool Enabled { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string SearchEngineId { get; set; } = string.Empty;
}

public sealed class PexelsOptions
{
    public bool Enabled { get; set; }
    public string ApiKey { get; set; } = string.Empty;
}

public sealed class PixabayOptions
{
    public bool Enabled { get; set; }
    public string ApiKey { get; set; } = string.Empty;
}

public sealed class UnsplashOptions
{
    public bool Enabled { get; set; }
    public string AccessKey { get; set; } = string.Empty;
}

public sealed class ScraperOptions
{
    public bool BaiduEnabled { get; set; } = true;
    public bool SogouEnabled { get; set; }
    public bool So360Enabled { get; set; } = true;
    public bool BingScrapeEnabled { get; set; }
    public bool DuckDuckGoEnabled { get; set; }
    public string UserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";
    public int RequestDelayMs { get; set; } = 300;
}

public sealed class DownloadOptions
{
    public string DefaultDirectory { get; set; } = string.Empty;
}
