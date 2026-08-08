using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageSpider.App.Services;
using ImageSpider.Core.Abstractions;
using ImageSpider.Core.Models;
using ImageSpider.Core.Options;
using ImageSpider.Core.Utilities;

namespace ImageSpider.App.ViewModels;

public partial class ImagePreviewViewModel : ObservableObject
{
    private readonly IImageDownloadService _downloadService;
    private readonly SpiderOptions _options;
    private readonly ImageItemViewModel _item;

    public ImageResultItem Item => _item.Item;
    public string Title => _item.Title;
    public string Source => _item.Source;
    public string SizeText => _item.SizeText;
    public string? ImageUrl { get; }

    [ObservableProperty]
    private BitmapImage? _previewImage;

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private string _statusText = "正在加载大图...";

    public ImagePreviewViewModel(
        ImageItemViewModel item,
        IImageDownloadService downloadService,
        SpiderOptions options)
    {
        _item = item;
        _downloadService = downloadService;
        _options = options;
        ImageUrl = ImageLoader.ResolveImageUrl(Item);
        PreviewImage = item.Thumbnail;
        _ = LoadPreviewAsync();
    }

    private async Task LoadPreviewAsync()
    {
        IsLoading = true;

        var url = ImageUrl;
        if (url is null)
        {
            StatusText = PreviewImage is null
                ? "无法加载：图片地址无效。"
                : "地址无效，已显示缩略图。";
            IsLoading = false;
            return;
        }

        var bitmap = await ImageLoader.LoadBitmapAsync(url, Item.Source);
        if (bitmap is not null)
        {
            PreviewImage = bitmap;
            StatusText = $"{Source} · {SizeText}";
        }
        else if (PreviewImage is null)
        {
            StatusText = "大图加载失败，请稍后重试。";
        }
        else
        {
            StatusText = "大图加载失败，已显示缩略图。";
        }

        IsLoading = false;
    }

    [RelayCommand]
    private async Task DownloadAsync()
    {
        var dir = string.IsNullOrWhiteSpace(_options.Download.DefaultDirectory)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "ImageSpider")
            : _options.Download.DefaultDirectory;

        try
        {
            StatusText = "正在下载...";
            var path = await _downloadService.DownloadAsync(Item, dir);
            StatusText = $"已保存到 {path}";
        }
        catch (Exception ex)
        {
            StatusText = $"下载失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenInBrowser()
    {
        var url = UrlHelper.FirstHttpUrl(Item.HostPageUrl, Item.ContentUrl, Item.ThumbnailUrl);
        if (url is null)
        {
            StatusText = "无法在浏览器中打开：无有效链接。";
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            StatusText = $"打开浏览器失败: {ex.Message}";
        }
    }
}
