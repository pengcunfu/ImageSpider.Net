using System.IO;
using System.Net.Http;
using System.Windows.Media.Imaging;
using ImageSpider.Core.Models;
using ImageSpider.Core.Utilities;
using ImageSpider.Infrastructure.Baidu;
using ImageSpider.Infrastructure.Http;

namespace ImageSpider.App.Services;

public static class ImageLoader
{
    private static readonly HttpClient Client = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public static string? ResolveImageUrl(ImageResultItem item) =>
        UrlHelper.FirstHttpUrl(item.ContentUrl, item.ThumbnailUrl)
        ?? BaiduUrlDecoder.Decode(item.ContentUrl)
        ?? BaiduUrlDecoder.Decode(item.ThumbnailUrl);

    public static async Task<BitmapImage?> LoadBitmapAsync(string url, ImageSourceKind source, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            RefererHelper.ApplyReferer(request, source);

            using var response = await Client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            await using var ms = new MemoryStream(bytes);
            var img = new BitmapImage();
            img.BeginInit();
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.StreamSource = ms;
            img.EndInit();
            img.Freeze();
            return img;
        }
        catch
        {
            return null;
        }
    }
}
