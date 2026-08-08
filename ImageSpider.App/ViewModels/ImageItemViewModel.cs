using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using ImageSpider.App.Services;
using ImageSpider.Core.Models;

namespace ImageSpider.App.ViewModels;

public partial class ImageItemViewModel : ObservableObject
{
    public ImageResultItem Item { get; }

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private BitmapImage? _thumbnail;

    [ObservableProperty]
    private bool _isLoading = true;

    public string Title => Item.Title;
    public string Source => Item.SourceDisplayName ?? Item.Source.ToString();
    public string SizeText => Item.Width is > 0 && Item.Height is > 0 ? $"{Item.Width}×{Item.Height}" : "—";
    public string ContentUrl => Item.ContentUrl;

    public ImageItemViewModel(ImageResultItem item)
    {
        Item = item;
        _ = LoadThumbnailAsync();
    }

    private async Task LoadThumbnailAsync()
    {
        try
        {
            var url = ImageLoader.ResolveImageUrl(Item) ?? Item.ThumbnailUrl;
            Thumbnail = await ImageLoader.LoadBitmapAsync(url, Item.Source);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
