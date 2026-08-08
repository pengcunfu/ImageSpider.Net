using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageSpider.App.Services;
using ImageSpider.App.Views;
using ImageSpider.Core.Abstractions;
using ImageSpider.Core.Models;
using ImageSpider.Core.Utilities;
using ImageSpider.Core.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace ImageSpider.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IImageSearchService _searchService;
    private readonly IImageDownloadService _downloadService;
    private readonly UserSettingsStore _settingsStore;
    private readonly IConfiguration _configuration;
    private SpiderOptions _options = new();
    private int _currentPage;
    private string _lastQuery = string.Empty;
    private bool _hasMore;

    [ObservableProperty]
    private string _query = string.Empty;

    [ObservableProperty] private bool _useBing = true;
    [ObservableProperty] private bool _useBaidu = true;
    [ObservableProperty] private bool _useSo360 = true;
    [ObservableProperty] private bool _useSogou;
    [ObservableProperty] private bool _useBingScrape;
    [ObservableProperty] private bool _useDuckDuckGo;
    [ObservableProperty] private bool _useGoogle;
    [ObservableProperty] private bool _usePexels;
    [ObservableProperty] private bool _usePixabay;
    [ObservableProperty] private bool _useUnsplash;
    [ObservableProperty] private bool _safeSearch = true;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusText = "就绪。勾选搜索来源后输入关键词。API 类需在设置中配置密钥。";

    public ObservableCollection<ImageItemViewModel> Results { get; } = [];

    public MainViewModel(
        IImageSearchService searchService,
        IImageDownloadService downloadService,
        UserSettingsStore settingsStore,
        IConfiguration configuration)
    {
        _searchService = searchService;
        _downloadService = downloadService;
        _settingsStore = settingsStore;
        _configuration = configuration;
        ReloadOptions();
    }

    public void ReloadOptions() => _options = _settingsStore.LoadMerged();

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(Query))
        {
            StatusText = "请输入搜索关键词。";
            return;
        }

        Results.Clear();
        _currentPage = 0;
        _lastQuery = Query.Trim();
        await RunSearchAsync(reset: true);
    }

    [RelayCommand]
    private async Task LoadMoreAsync()
    {
        if (!_hasMore || IsBusy || string.IsNullOrWhiteSpace(_lastQuery))
            return;

        _currentPage++;
        await RunSearchAsync(reset: false);
    }

    private async Task RunSearchAsync(bool reset)
    {
        IsBusy = true;
        StatusText = reset ? "搜索中..." : "加载更多...";

        try
        {
            var sources = BuildSources();
            if (sources.Count == 0)
            {
                StatusText = "请至少勾选一个搜索来源。";
                return;
            }

            var request = new ImageSearchRequest
            {
                Query = _lastQuery,
                Page = _currentPage,
                PageSize = 30,
                Sources = sources,
                SafeSearch = SafeSearch
            };

            var response = await _searchService.SearchAsync(request);
            _hasMore = response.HasMore;

            foreach (var item in response.Items)
                Results.Add(new ImageItemViewModel(item));

            var errorHint = response.Errors.Count > 0 ? $" | 提示: {string.Join("; ", response.Errors)}" : "";
            StatusText = $"共 {Results.Count} 张" + (_hasMore ? "，可加载更多" : "") + errorHint;
        }
        catch (Exception ex)
        {
            StatusText = $"搜索失败: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void OpenSettings()
    {
        var vm = new SettingsViewModel();
        vm.LoadFrom(_options);
        var win = new SettingsWindow(vm)
        {
            Owner = Application.Current.MainWindow
        };
        if (win.ShowDialog() == true)
        {
            _settingsStore.Save(vm.ToOptions());
            if (_configuration is IConfigurationRoot root)
                root.Reload();
            ReloadOptions();
            StatusText = "设置已保存。";
        }
    }

    [RelayCommand]
    private async Task DownloadSelectedAsync()
    {
        var selected = Results.Where(r => r.IsSelected).Select(r => r.Item).ToList();
        if (selected.Count == 0)
        {
            StatusText = "请先勾选要下载的图片。";
            return;
        }

        var dir = string.IsNullOrWhiteSpace(_options.Download.DefaultDirectory)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "ImageSpider")
            : _options.Download.DefaultDirectory;

        IsBusy = true;
        try
        {
            var paths = await _downloadService.DownloadManyAsync(
                selected,
                dir,
                new Progress<(int Done, int Total)>(p => StatusText = $"下载中 {p.Done}/{p.Total}..."));

            StatusText = $"已下载 {paths.Count}/{selected.Count} 张到 {dir}";
        }
        catch (Exception ex)
        {
            StatusText = $"下载失败: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ShowPreview(ImageItemViewModel? item)
    {
        if (item is null)
            return;

        var vm = new ImagePreviewViewModel(item, _downloadService, _options);
        var win = new ImagePreviewWindow(vm)
        {
            Owner = Application.Current.MainWindow
        };
        win.ShowDialog();
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var r in Results)
            r.IsSelected = true;
    }

    [RelayCommand]
    private void ClearSelection()
    {
        foreach (var r in Results)
            r.IsSelected = false;
    }

    private List<ImageSourceKind> BuildSources()
    {
        var list = new List<ImageSourceKind>();
        if (UseBing && _options.Bing.Enabled)
            list.Add(ImageSourceKind.BingApi);
        if (UseBaidu && _options.Scraper.BaiduEnabled)
            list.Add(ImageSourceKind.BaiduScrape);
        if (UseSo360 && _options.Scraper.So360Enabled)
            list.Add(ImageSourceKind.So360Scrape);
        if (UseSogou && _options.Scraper.SogouEnabled)
            list.Add(ImageSourceKind.SogouScrape);
        if (UseBingScrape && _options.Scraper.BingScrapeEnabled)
            list.Add(ImageSourceKind.BingScrape);
        if (UseDuckDuckGo && _options.Scraper.DuckDuckGoEnabled)
            list.Add(ImageSourceKind.DuckDuckGoScrape);
        if (UseGoogle && _options.Google.Enabled)
            list.Add(ImageSourceKind.GoogleApi);
        if (UsePexels && _options.Pexels.Enabled)
            list.Add(ImageSourceKind.PexelsApi);
        if (UsePixabay && _options.Pixabay.Enabled)
            list.Add(ImageSourceKind.PixabayApi);
        if (UseUnsplash && _options.Unsplash.Enabled)
            list.Add(ImageSourceKind.UnsplashApi);
        return list;
    }
}
