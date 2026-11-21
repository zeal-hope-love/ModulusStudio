using System.ComponentModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using ModulusStudio.App.WinUI.ViewModels;
using ModulusStudio.Core.Packing;
using Windows.UI;

namespace ModulusStudio.App.WinUI.Views;

public sealed partial class TextModulusPage : Page
{
    private const int DemoGridSize = 12;

    private static readonly Color CellBaseColor = Color.FromArgb(0xFF, 0xF7, 0xF9, 0xFC);
    private static readonly Color CellPaintColor = Color.FromArgb(0xFF, 0x3B, 0x82, 0xF6);
    private static readonly Color TileNormalColor = Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF);
    private static readonly Color TileSelectedColor = Color.FromArgb(0xFF, 0xE8, 0xF1, 0xFF);
    private static readonly Color BorderNormalColor = Color.FromArgb(0xFF, 0xE1, 0xE5, 0xEC);
    private static readonly Color BorderSelectedColor = Color.FromArgb(0xFF, 0x2F, 0x6F, 0xEB);

    private readonly TextModulusViewModel _viewModel = new();
    private readonly DispatcherQueueTimer _tileTimer;
    private readonly List<ModeTileState> _tiles = new();

    private sealed class ModeTileState
    {
        public Border Tile { get; init; } = null!;
        public Rectangle[,] Cells { get; init; } = null!;
        public List<(int X, int Y)> Order { get; init; } = null!;
        public ScanMode Mode { get; init; }
        public int Progress { get; set; }
    }

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

        BuildModeTiles();
        UpdateModeSelection();

        _tileTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _tileTimer.Interval = TimeSpan.FromMilliseconds(60);
        _tileTimer.Tick += (_, _) => AdvanceTiles();

        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        Loaded += (_, _) =>
        {
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            UpdateModeSelection();
            _tileTimer.Start();
        };
        Unloaded += (_, _) => _tileTimer.Stop();
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
            case nameof(TextModulusViewModel.CurrentScanMode):
                UpdateModeSelection();
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

    #region 取模方式演示动画

    private void BuildModeTiles()
    {
        AddTile(ModeTileColumnRow, ModeDemoColumnRow, ScanMode.ColumnRow);
        AddTile(ModeTileRowColumn, ModeDemoRowColumn, ScanMode.RowColumn);
        AddTile(ModeTileColumnMajor, ModeDemoColumnMajor, ScanMode.ColumnMajor);
        AddTile(ModeTileRowMajor, ModeDemoRowMajor, ScanMode.RowMajor);

        void AddTile(Border tile, Grid host, ScanMode mode)
        {
            for (var i = 0; i < DemoGridSize; i++)
            {
                host.ColumnDefinitions.Add(new ColumnDefinition());
                host.RowDefinitions.Add(new RowDefinition());
            }

            var cells = new Rectangle[DemoGridSize, DemoGridSize];
            for (var y = 0; y < DemoGridSize; y++)
            for (var x = 0; x < DemoGridSize; x++)
            {
                var rect = new Rectangle
                {
                    Fill = new SolidColorBrush(CellBaseColor),
                    Margin = new Thickness(0.35)
                };
                Grid.SetRow(rect, y);
                Grid.SetColumn(rect, x);
                host.Children.Add(rect);
                cells[x, y] = rect;
            }

            _tiles.Add(new ModeTileState
            {
                Tile = tile,
                Cells = cells,
                Order = BuildOrder(mode),
                Mode = mode
            });
        }
    }

    /// <summary>12×12 的取模顺序，与网站的演示画布保持一致（每 8 个为一组）。</summary>
    private static List<(int X, int Y)> BuildOrder(ScanMode mode)
    {
        var order = new List<(int X, int Y)>(DemoGridSize * DemoGridSize);
        const int n = DemoGridSize;

        switch (mode)
        {
            case ScanMode.ColumnRow:
                for (var y0 = 0; y0 < n; y0 += 8)
                for (var x = 0; x < n; x++)
                for (var y = y0; y < Math.Min(y0 + 8, n); y++)
                    order.Add((x, y));
                break;

            case ScanMode.RowColumn:
                for (var x0 = 0; x0 < n; x0 += 8)
                for (var y = 0; y < n; y++)
                for (var x = x0; x < Math.Min(x0 + 8, n); x++)
                    order.Add((x, y));
                break;

            case ScanMode.ColumnMajor:
                for (var x = 0; x < n; x++)
                for (var y = 0; y < n; y++)
                    order.Add((x, y));
                break;

            case ScanMode.RowMajor:
            default:
                for (var y = 0; y < n; y++)
                for (var x = 0; x < n; x++)
                    order.Add((x, y));
                break;
        }

        return order;
    }

    private void AdvanceTiles()
    {
        var brush = new SolidColorBrush(CellPaintColor);
        var baseBrush = new SolidColorBrush(CellBaseColor);

        foreach (var tile in _tiles)
        {
            if (tile.Progress >= tile.Order.Count)
            {
                foreach (var cell in tile.Cells)
                    cell.Fill = baseBrush;
                tile.Progress = 0;
            }

            var (x, y) = tile.Order[tile.Progress];
            tile.Cells[x, y].Fill = brush;
            tile.Progress++;
        }
    }

    private void ModeTile_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is not Border { Tag: string tag }) return;

        switch (tag)
        {
            case "columnRow": _viewModel.IsColumnRow = true; break;
            case "rowColumn": _viewModel.IsRowColumn = true; break;
            case "columnMajor": _viewModel.IsColumnMajor = true; break;
            case "rowMajor": _viewModel.IsRowMajor = true; break;
        }

        UpdateModeSelection();
    }

    private void UpdateModeSelection()
    {
        var selected = _viewModel.CurrentScanMode;
        foreach (var tile in _tiles)
        {
            var on = tile.Mode == selected;
            tile.Tile.BorderBrush = new SolidColorBrush(on ? BorderSelectedColor : BorderNormalColor);
            tile.Tile.BorderThickness = new Thickness(on ? 2 : 1);
            tile.Tile.Background = new SolidColorBrush(on ? TileSelectedColor : TileNormalColor);
        }
    }

    #endregion
}
