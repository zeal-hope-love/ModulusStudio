using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ModulusStudio.App.WinUI.ViewModels;

namespace ModulusStudio.App.WinUI.Views;

public sealed partial class TextModulusPage : Page
{
    private readonly TextModulusViewModel _viewModel = new();

    public TextModulusPage()
    {
        InitializeComponent();
        DataContext = _viewModel;

        CellWidthBox.Value = _viewModel.CellWidth;
        CellHeightBox.Value = _viewModel.CellHeight;
        OffsetXBox.Value = _viewModel.OffsetX;
        OffsetYBox.Value = _viewModel.OffsetY;
        BytesPerLineBox.Value = _viewModel.BytesPerLine;

        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        Unloaded += (_, _) =>
        {
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            _viewModel.Dispose();
        };
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(TextModulusViewModel.CellWidth):
                SyncNumber(CellWidthBox, _viewModel.CellWidth);
                break;
            case nameof(TextModulusViewModel.CellHeight):
                SyncNumber(CellHeightBox, _viewModel.CellHeight);
                break;
            case nameof(TextModulusViewModel.OffsetX):
                SyncNumber(OffsetXBox, _viewModel.OffsetX);
                break;
            case nameof(TextModulusViewModel.OffsetY):
                SyncNumber(OffsetYBox, _viewModel.OffsetY);
                break;
            case nameof(TextModulusViewModel.BytesPerLine):
                SyncNumber(BytesPerLineBox, _viewModel.BytesPerLine);
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
            case "cellWidth": _viewModel.CellWidth = value; break;
            case "cellHeight": _viewModel.CellHeight = value; break;
            case "offsetX": _viewModel.OffsetX = value; break;
            case "offsetY": _viewModel.OffsetY = value; break;
            case "bytesPerLine": _viewModel.BytesPerLine = value; break;
        }
    }

    private void InputText_TextChanged(object sender, TextChangedEventArgs e)
        => _viewModel.InputText = InputTextBox.Text;
}
