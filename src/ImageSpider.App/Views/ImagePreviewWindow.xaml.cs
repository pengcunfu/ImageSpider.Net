using System.Windows;
using ImageSpider.App.ViewModels;

namespace ImageSpider.App.Views;

public partial class ImagePreviewWindow : Window
{
    public ImagePreviewWindow(ImagePreviewViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Title = string.IsNullOrWhiteSpace(viewModel.Title) ? "图片预览" : viewModel.Title;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
