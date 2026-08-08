using System.Windows;
using System.Windows.Input;
using ImageSpider.App.ViewModels;

namespace ImageSpider.App;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void SearchBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is MainViewModel vm && vm.SearchCommand.CanExecute(null))
            vm.SearchCommand.Execute(null);
    }

    private void Thumbnail_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is ImageItemViewModel item
            && DataContext is MainViewModel vm)
        {
            vm.ShowPreviewCommand.Execute(item);
        }
    }
}
