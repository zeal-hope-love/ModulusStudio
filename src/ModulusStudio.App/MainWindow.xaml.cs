using System.Windows;
using System.Windows.Controls;
using ModulusStudio.App.ViewModels;

namespace ModulusStudio.App;

public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly TextModulusViewModel _textViewModel = new();
    private readonly ImageModulusViewModel _imageViewModel = new();

    public MainWindow()
    {
        InitializeComponent();

        TextView.DataContext = _textViewModel;
        ImageView.DataContext = _imageViewModel;
        ApplyNavigation();
    }

    private void NavList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyNavigation();
    }

    private void ApplyNavigation()
    {
        if (TextView is null || ImageView is null) return;
        var index = NavList.SelectedIndex;
        TextView.Visibility = index == 0 ? Visibility.Visible : Visibility.Collapsed;
        ImageView.Visibility = index == 1 ? Visibility.Visible : Visibility.Collapsed;
    }

    protected override void OnClosed(EventArgs e)
    {
        _textViewModel.Dispose();
        base.OnClosed(e);
    }
}
