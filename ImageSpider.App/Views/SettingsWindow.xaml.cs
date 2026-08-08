using System.IO;
using System.Windows;
using Microsoft.Win32;
using ImageSpider.App.ViewModels;

namespace ImageSpider.App.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Loaded += (_, _) => BingKeyBox.Password = viewModel.BingSubscriptionKey;
    }

    private void BrowseDownloadDir_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "选择默认下载文件夹",
            InitialDirectory = Directory.Exists(_viewModel.DownloadDirectory)
                ? _viewModel.DownloadDirectory
                : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
        };

        if (dialog.ShowDialog() == true)
            _viewModel.DownloadDirectory = dialog.FolderName;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.BingSubscriptionKey = BingKeyBox.Password;
        DialogResult = true;
        Close();
    }
}
