using ImageSpider.Core.Models;

namespace ImageSpider.Core.Abstractions;

public interface IImageDownloadService
{
    Task<string> DownloadAsync(ImageResultItem item, string targetDirectory, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> DownloadManyAsync(IEnumerable<ImageResultItem> items, string targetDirectory, IProgress<(int Done, int Total)>? progress = null, CancellationToken cancellationToken = default);
}
