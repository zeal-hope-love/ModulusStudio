using System.Windows;
using System.Windows.Controls;
using ModulusStudio.App.ViewModels;

namespace ModulusStudio.App.Views;

public partial class ImageModulusView : UserControl
{
    public ImageModulusView()
    {
        InitializeComponent();
    }

    private void DropArea_OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void DropArea_OnDrop(object sender, DragEventArgs e)
    {
        if (DataContext is not ImageModulusViewModel vm) return;
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
            vm.LoadFile(files[0]);
    }
}
