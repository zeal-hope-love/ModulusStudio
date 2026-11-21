using System.ComponentModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using ModulusStudio.App.WinUI.ViewModels;
using ModulusStudio.Core.Models;
using ModulusStudio.Core.Packing;
using Windows.UI;

namespace ModulusStudio.App.WinUI.Views;

public sealed partial class TextModulusPage : Page
{
    private static readonly Color OffColor = Color.FromArgb(0xFF, 0xEA, 0xEE, 0xF4);
    private static readonly Color OnColor = Color.FromArgb(0xFF, 0x7A, 0x84, 0x94);
    private static readonly Color VisitedColor = Color.FromArgb(0xFF, 0xC5, 0xDB, 0xFA);
    private static readonly Color CurrentColor = Color.FromArgb(0xFF, 0x2F, 0x6F, 0xEB);

    private readonly TextModulusViewModel _viewModel = new();
    private readonly DispatcherQueueTimer _demoTimer;

    private Rectangle[,]? _demoCells;
    private List<List<(int X, int Y)>> _demoGroups = new();
    private MonoBitmap? _demoBitmap;
    private int _demoStep = -1;

    public TextModulusPage()
    {
        InitializeComponent();
        DataContext = _viewModel;

        CellWidthBox.Value = _viewModel.CellWidth;
        CellHeightBox.Value = _viewModel.CellHeight;
        OffsetXBox.Value = _viewModel.OffsetX;
        OffsetYBox.Value = _viewModel.OffsetY;
        BytesPerLineBox.Value = _viewModel.BytesPerLine;
        TemplateBox.Text = _viewModel.TemplateText;

        TemplateBox.TextChanged += TemplateBox_TextChanged;

        _demoTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _demoTimer.Interval = TimeSpan.FromMilliseconds(260);
        _demoTimer.Tick += (_, _) => AdvanceDemo();

        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        Loaded += (_, _) =>
        {
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            RebuildDemo();
            _demoTimer.Start();
        };
        Unloaded += (_, _) => _demoTimer.Stop();
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
            case nameof(TextModulusViewModel.DemoBitmap):
                RebuildDemo();
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

    private void TemplateBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_viewModel.TemplateText != TemplateBox.Text)
            _viewModel.TemplateText = TemplateBox.Text;
    }

    #region 取模演示动画

    private void RebuildDemo()
    {
        _demoBitmap = _viewModel.DemoBitmap;
        _demoCells = null;
        _demoGroups.Clear();
        _demoStep = -1;
        DemoGrid.Children.Clear();
        DemoGrid.ColumnDefinitions.Clear();
        DemoGrid.RowDefinitions.Clear();
        DemoCaption.Text = string.Empty;

        if (_demoBitmap is null || _demoBitmap.Width <= 0 || _demoBitmap.Height <= 0) return;

        const double cell = 10;
        DemoGrid.Width = _demoBitmap.Width * cell;
        DemoGrid.Height = _demoBitmap.Height * cell;

        for (var x = 0; x < _demoBitmap.Width; x++)
            DemoGrid.ColumnDefinitions.Add(new ColumnDefinition());
        for (var y = 0; y < _demoBitmap.Height; y++)
            DemoGrid.RowDefinitions.Add(new RowDefinition());

        _demoCells = new Rectangle[_demoBitmap.Width, _demoBitmap.Height];
        for (var y = 0; y < _demoBitmap.Height; y++)
        for (var x = 0; x < _demoBitmap.Width; x++)
        {
            var rect = new Rectangle
            {
                Margin = new Thickness(0.6),
                RadiusX = 1,
                RadiusY = 1,
                Fill = new SolidColorBrush(_demoBitmap.IsOn(x, y) ? OnColor : OffColor)
            };
            Grid.SetRow(rect, y);
            Grid.SetColumn(rect, x);
            DemoGrid.Children.Add(rect);
            _demoCells[x, y] = rect;
        }

        _demoGroups = BuildGroups(_viewModel.CurrentScanMode, _demoBitmap.Width, _demoBitmap.Height);
        if (_demoGroups.Count > 0) AdvanceDemo();
    }

    private void AdvanceDemo()
    {
        if (_demoCells is null || _demoGroups.Count == 0) return;

        if (_demoStep >= 0 && _demoStep < _demoGroups.Count)
            PaintGroup(_demoStep, VisitedColor);

        _demoStep++;
        if (_demoStep >= _demoGroups.Count)
        {
            _demoStep = 0;
            ResetBaseColors();
        }

        PaintGroup(_demoStep, CurrentColor);
        var value = _viewModel.DemoBytes.Count > 0 ? _viewModel.DemoBytes[_demoStep] : (byte)0;
        DemoCaption.Text = $"第 {_demoStep + 1}/{_demoGroups.Count} 字节  0x{value:X2}";
    }

    private void PaintGroup(int index, Color color)
    {
        if (_demoCells is null) return;
        var brush = new SolidColorBrush(color);
        foreach (var (x, y) in _demoGroups[index])
            _demoCells[x, y].Fill = brush;
    }

    private void ResetBaseColors()
    {
        if (_demoCells is null || _demoBitmap is null) return;
        for (var y = 0; y < _demoBitmap.Height; y++)
        for (var x = 0; x < _demoBitmap.Width; x++)
        {
            _demoCells[x, y].Fill = new SolidColorBrush(
                _demoBitmap.IsOn(x, y) ? OnColor : OffColor);
        }
    }

    /// <summary>按取模方式还原每个字节对应的像素分组，与 BitmapPacker 的打包顺序一致。</summary>
    private static List<List<(int X, int Y)>> BuildGroups(ScanMode mode, int width, int height)
    {
        var groups = new List<List<(int X, int Y)>>();
        var xBands = (width + 7) / 8;
        var yBands = (height + 7) / 8;

        List<(int X, int Y)> Group(Func<int, bool> valid, Func<int, (int X, int Y)> cell)
        {
            var cells = new List<(int X, int Y)>(8);
            for (var i = 0; i < 8; i++)
                if (valid(i)) cells.Add(cell(i));
            return cells;
        }

        switch (mode)
        {
            case ScanMode.RowMajor:
                for (var y = 0; y < height; y++)
                for (var bx = 0; bx < xBands; bx++)
                    groups.Add(Group(i => bx * 8 + i < width, i => (bx * 8 + i, y)));
                break;

            case ScanMode.ColumnMajor:
                for (var x = 0; x < width; x++)
                for (var by = 0; by < yBands; by++)
                    groups.Add(Group(i => by * 8 + i < height, i => (x, by * 8 + i)));
                break;

            case ScanMode.ColumnRow:
                for (var by = 0; by < yBands; by++)
                for (var x = 0; x < width; x++)
                    groups.Add(Group(i => by * 8 + i < height, i => (x, by * 8 + i)));
                break;

            case ScanMode.RowColumn:
                for (var bx = 0; bx < xBands; bx++)
                for (var y = 0; y < height; y++)
                    groups.Add(Group(i => bx * 8 + i < width, i => (bx * 8 + i, y)));
                break;
        }

        return groups;
    }

    #endregion
}
