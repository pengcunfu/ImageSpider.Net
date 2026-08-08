using System.IO;
using System.Net.Http;
using ImageSpider.Core.Abstractions;
using ImageSpider.Core.Models;
using ImageSpider.Core.Utilities;
using ImageSpider.Infrastructure.Baidu;
using ImageSpider.Infrastructure.Http;

namespace ImageSpider.Infrastructure.Services;

public sealed class ImageDownloadService : IImageDownloadService
{
    private readonly SpiderHttpClientFactory _clientFactory;

    public ImageDownloadService(SpiderHttpClientFactory clientFactory) =>
        _clientFactory = clientFactory;

    public async Task<string> DownloadAsync(ImageResultItem item, string targetDirectory, CancellationToken cancellationToken = default)
    {
        var url = UrlHelper.FirstHttpUrl(item.ContentUrl, item.ThumbnailUrl)
            ?? BaiduUrlDecoder.Decode(item.ContentUrl)
            ?? throw new InvalidOperationException("图片地址无效，无法下载。");

        Directory.CreateDirectory(targetDirectory);
        var fileName = BuildFileName(item);
        var path = Path.Combine(targetDirectory, fileName);

        using var client = _clientFactory.CreateScraperClient();
        RefererHelper.ApplyReferer(client, item.Source);

        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var file = File.Create(path);
        await stream.CopyToAsync(file, cancellationToken);

        return path;
    }

    public async Task<IReadOnlyList<string>> DownloadManyAsync(
        IEnumerable<ImageResultItem> items,
        string targetDirectory,
        IProgress<(int Done, int Total)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var list = items.ToList();
        var paths = new List<string>();
        var done = 0;

        foreach (var item in list)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var path = await DownloadAsync(item, targetDirectory, cancellationToken);
                paths.Add(path);
            }
            catch
            {
                // skip failed item
            }

            done++;
            progress?.Report((done, list.Count));
        }

        return paths;
    }

    private static string BuildFileName(ImageResultItem item)
    {
        var ext = GuessExtension(item.ContentUrl);
        var safeTitle = string.Join("_", item.Title.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        if (safeTitle.Length > 40)
            safeTitle = safeTitle[..40];
        if (string.IsNullOrWhiteSpace(safeTitle))
            safeTitle = "image";

        var hash = Math.Abs(item.ContentUrl.GetHashCode()).ToString("X8");
        return $"{safeTitle}_{hash}{ext}";
    }

    private static string GuessExtension(string url)
    {
        try
        {
            var path = new Uri(url).AbsolutePath;
            var ext = Path.GetExtension(path);
            if (!string.IsNullOrWhiteSpace(ext) && ext.Length <= 5)
                return ext;
        }
        catch
        {
            // ignore
        }

        return ".jpg";
    }
}
