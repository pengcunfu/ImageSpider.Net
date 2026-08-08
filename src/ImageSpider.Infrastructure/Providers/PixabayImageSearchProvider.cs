using System.Text.Json;
using ImageSpider.Core.Abstractions;
using ImageSpider.Core.Models;
using ImageSpider.Core.Options;
using ImageSpider.Infrastructure.Json;
using Microsoft.Extensions.Options;

namespace ImageSpider.Infrastructure.Providers;

public sealed class PixabayImageSearchProvider : IImageSearchProvider
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<SpiderOptions> _optionsMonitor;

    public PixabayImageSearchProvider(HttpClient httpClient, IOptionsMonitor<SpiderOptions> optionsMonitor)
    {
        _httpClient = httpClient;
        _optionsMonitor = optionsMonitor;
    }

    private PixabayOptions _options => _optionsMonitor.CurrentValue.Pixabay;

    public ImageSourceKind Source => ImageSourceKind.PixabayApi;
    public string DisplayName => "Pixabay";
    public bool IsConfigured => _options.Enabled && !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<ImageSearchResponse> SearchAsync(ImageSearchRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return new ImageSearchResponse
            {
                Items = [],
                HasMore = false,
                Errors = ["Pixabay 未配置：请在设置中填写 API Key。"]
            };
        }

        var page = request.Page + 1;
        var query = Uri.EscapeDataString(request.Query.Trim());
        var safe = request.SafeSearch ? "true" : "false";
        var url = $"https://pixabay.com/api/?key={_options.ApiKey}&q={query}&image_type=photo&per_page={request.PageSize}&page={page}&safesearch={safe}";

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new ImageSearchResponse
            {
                Items = [],
                HasMore = false,
                Errors = [$"Pixabay API 失败 ({(int)response.StatusCode})"]
            };
        }

        var items = new List<ImageResultItem>();
        using var doc = JsonDocument.Parse(body);

        if (doc.RootElement.TryGetProperty("hits", out var hits))
        {
            foreach (var node in hits.EnumerateArray())
            {
                var content = JsonElementHelper.GetString(node, "largeImageURL") ?? JsonElementHelper.GetString(node, "webformatURL");
                if (string.IsNullOrWhiteSpace(content))
                    continue;

                items.Add(new ImageResultItem
                {
                    Id = $"pixabay-{JsonElementHelper.GetInt(node, "id") ?? 0}",
                    Title = JsonElementHelper.GetString(node, "tags") ?? request.Query,
                    ThumbnailUrl = JsonElementHelper.GetString(node, "previewURL") ?? content,
                    ContentUrl = content,
                    HostPageUrl = JsonElementHelper.GetString(node, "pageURL"),
                    Width = JsonElementHelper.GetInt(node, "imageWidth"),
                    Height = JsonElementHelper.GetInt(node, "imageHeight"),
                    Source = ImageSourceKind.PixabayApi,
                    SourceDisplayName = DisplayName
                });
            }
        }

        var total = JsonElementHelper.GetInt(doc.RootElement, "totalHits") ?? 0;
        var hasMore = page * request.PageSize < total;

        return new ImageSearchResponse { Items = items, HasMore = hasMore };
    }
}
