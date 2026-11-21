using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ModulusStudio.App.WinUI.ViewModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace ModulusStudio.App.WinUI.Views;

public sealed partial class ImageModulusPage : Page
{
    private readonly ImageModulusViewModel _viewModel = new();

    public ImageModulusPage()
    {
        InitializeComponent();
        DataContext = _viewModel;

        BytesPerLineBox.Value = _viewModel.BytesPerLine;
        ThresholdBox.Value = _viewModel.Threshold;
        BrightnessBox.Value = _viewModel.Brightness;
        ContrastBox.Value = _viewModel.Contrast;
        MaxWidthBox.Value = _viewModel.MaxWidth;
        MaxHeightBox.Value = _viewModel.MaxHeight;

        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        Unloaded += (_, _) => _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ImageModulusViewModel.BytesPerLine):
                SyncNumber(BytesPerLineBox, _viewModel.BytesPerLine);
                break;
            case nameof(ImageModulusViewModel.Threshold):
                SyncNumber(ThresholdBox, _viewModel.Threshold);
                break;
            case nameof(ImageModulusViewModel.Brightness):
                SyncNumber(BrightnessBox, _viewModel.Brightness);
                break;
            case nameof(ImageModulusViewModel.Contrast):
                SyncNumber(ContrastBox, _viewModel.Contrast);
                break;
            case nameof(ImageModulusViewModel.MaxWidth):
                SyncNumber(MaxWidthBox, _viewModel.MaxWidth);
                break;
            case nameof(ImageModulusViewModel.MaxHeight):
                SyncNumber(MaxHeightBox, _viewModel.MaxHeight);
                break;
        }
    }

    private static void SyncNumber(NumberBox box, int value)
    {
        if (double.IsNaN(box.Value) || Math.Abs(box.Value - value) > 0.001)
            box.Value = value;
    }

    private void NumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (double.IsNaN(args.NewValue)) return;
        var value = (int)Math.Round(args.NewValue);

        switch (sender.Tag as string)
        {
            case "bytesPerLine": _viewModel.BytesPerLine = value; break;
            case "threshold": _viewModel.Threshold = value; break;
            case "brightness": _viewModel.Brightness = value; break;
            case "contrast": _viewModel.Contrast = value; break;
            case "maxWidth": _viewModel.MaxWidth = value; break;
            case "maxHeight": _viewModel.MaxHeight = value; break;
        }
    }

    private void DropArea_DragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "打开图片";
            e.DragUIOverride.IsCaptionVisible = true;
        }
    }

    private async void DropArea_Drop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;

        var items = await e.DataView.GetStorageItemsAsync();
        if (items.FirstOrDefault() is StorageFile file)
            await _viewModel.LoadFileAsync(file);
    }
}
