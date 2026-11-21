using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using Microsoft.UI.Xaml.Media;
using ModulusStudio.App.WinUI.Mvvm;
using ModulusStudio.App.WinUI.Services;
using ModulusStudio.Core.Fonts;
using ModulusStudio.Core.Formatting;
using ModulusStudio.Core.Models;
using ModulusStudio.Core.Packing;
using ModulusStudio.Fonts;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
using Windows.UI;

namespace ModulusStudio.App.WinUI.ViewModels;

public sealed class TextModulusViewModel : ObservableObject, IDisposable
{
    private readonly GdiFontRasterizer _gdi = new();
    private readonly SkiaFontRasterizer _skia = new();
    private readonly List<byte[]> _glyphBytes = new();
    private readonly List<string> _glyphChars = new();
    private bool _suppress;

    public TextModulusViewModel()
    {
        FontFamilies = new ObservableCollection<string>(FontCatalog.GetFamilies());
        SelectedFont = FontFamilies.Contains("宋体") ? "宋体" : FontFamilies.FirstOrDefault() ?? "宋体";

        RegenerateCommand = new RelayCommand(Regenerate);
        CopyCommand = new RelayCommand(CopyCode);
        SaveTextCommand = new RelayCommand(() => _ = RunAsync(SaveTextAsync));
        SaveBinaryCommand = new RelayCommand(() => _ = RunAsync(SaveBinaryAsync));
        SaveLibraryCommand = new RelayCommand(() => _ = RunAsync(SaveLibraryAsync));
        ResetTemplateCommand = new RelayCommand(() => TemplateText = CodeTemplate.GetDefault(FormatIndex));

        Regenerate();
    }

    public ObservableCollection<string> FontFamilies { get; }

    public IReadOnlyList<string> FormatNames { get; } = new[] { "C51", "A51", "自定义" };

    public IReadOnlyList<string> RasterizerNames { get; } = new[] { "GDI（推荐，接近原软件）", "Skia / FreeType（轮廓）" };

    public IReadOnlyList<string> CharsetNames { get; } = new[] { "中文（GB2312）", "西文（ANSI）" };

    public RelayCommand RegenerateCommand { get; }
    public RelayCommand CopyCommand { get; }
    public RelayCommand SaveTextCommand { get; }
    public RelayCommand SaveBinaryCommand { get; }
    public RelayCommand SaveLibraryCommand { get; }
    public RelayCommand ResetTemplateCommand { get; }

    private string _templateText = CodeTemplate.C51;

    /// <summary>当前输出模板，支持直接编辑，改动后立即重新生成。</summary>
    public string TemplateText
    {
        get => _templateText;
        set { if (SetProperty(ref _templateText, value ?? string.Empty)) Regenerate(); }
    }

    /// <summary>演示动画使用的首个字符点阵。</summary>
    public MonoBitmap? DemoBitmap { get; private set; }

    /// <summary>演示动画使用的首个字符字模字节。</summary>
    public IReadOnlyList<byte> DemoBytes { get; private set; } = Array.Empty<byte>();

    /// <summary>当前取模方式，供界面绘制演示动画。</summary>
    public ScanMode CurrentScanMode => _scanMode;

    #region 字体参数

    private string _selectedFont = "宋体";
    public string SelectedFont
    {
        get => _selectedFont;
        set { if (SetProperty(ref _selectedFont, value)) Regenerate(); }
    }

    private int _cellWidth = 16;
    public int CellWidth
    {
        get => _cellWidth;
        set
        {
            var v = Math.Clamp(value, 3, 256);
            if (!SetProperty(ref _cellWidth, v)) return;
            if (_lockAspect)
            {
                _suppress = true;
                CellHeight = v;
                _suppress = false;
            }
            OnPropertyChanged(nameof(CellSummary));
            Regenerate();
        }
    }

    private int _cellHeight = 16;
    public int CellHeight
    {
        get => _cellHeight;
        set
        {
            var v = Math.Clamp(value, 3, 256);
            if (!SetProperty(ref _cellHeight, v)) return;
            if (_lockAspect && !_suppress)
            {
                _suppress = true;
                CellWidth = v;
                _suppress = false;
            }
            OnPropertyChanged(nameof(CellSummary));
            Regenerate();
        }
    }

    private bool _lockAspect = true;
    public bool LockAspect
    {
        get => _lockAspect;
        set
        {
            if (!SetProperty(ref _lockAspect, value)) return;
            if (value && CellWidth != CellHeight) CellHeight = CellWidth;
        }
    }

    private bool _fixedWidth = true;
    public bool FixedWidth
    {
        get => _fixedWidth;
        set { if (SetProperty(ref _fixedWidth, value)) Regenerate(); }
    }

    private bool _bold;
    public bool Bold
    {
        get => _bold;
        set { if (SetProperty(ref _bold, value)) Regenerate(); }
    }

    private bool _italic;
    public bool Italic
    {
        get => _italic;
        set { if (SetProperty(ref _italic, value)) Regenerate(); }
    }

    private bool _underline;
    public bool Underline
    {
        get => _underline;
        set { if (SetProperty(ref _underline, value)) Regenerate(); }
    }

    private int _offsetX;
    public int OffsetX
    {
        get => _offsetX;
        set { if (SetProperty(ref _offsetX, value)) Regenerate(); }
    }

    private int _offsetY;
    public int OffsetY
    {
        get => _offsetY;
        set { if (SetProperty(ref _offsetY, value)) Regenerate(); }
    }

    private int _charsetIndex;
    public int CharsetIndex
    {
        get => _charsetIndex;
        set { if (SetProperty(ref _charsetIndex, value)) Regenerate(); }
    }

    private int _rasterizerIndex;
    public int RasterizerIndex
    {
        get => _rasterizerIndex;
        set { if (SetProperty(ref _rasterizerIndex, value)) Regenerate(); }
    }

    #endregion

    #region 取模参数

    private ScanMode _scanMode = ScanMode.ColumnMajor;

    public bool IsRowMajor
    {
        get => _scanMode == ScanMode.RowMajor;
        set { if (value) SetScanMode(ScanMode.RowMajor); }
    }

    public bool IsColumnMajor
    {
        get => _scanMode == ScanMode.ColumnMajor;
        set { if (value) SetScanMode(ScanMode.ColumnMajor); }
    }

    public bool IsColumnRow
    {
        get => _scanMode == ScanMode.ColumnRow;
        set { if (value) SetScanMode(ScanMode.ColumnRow); }
    }

    public bool IsRowColumn
    {
        get => _scanMode == ScanMode.RowColumn;
        set { if (value) SetScanMode(ScanMode.RowColumn); }
    }

    private void SetScanMode(ScanMode mode)
    {
        if (_scanMode == mode) return;
        _scanMode = mode;
        OnPropertyChanged(nameof(IsRowMajor));
        OnPropertyChanged(nameof(IsColumnMajor));
        OnPropertyChanged(nameof(IsColumnRow));
        OnPropertyChanged(nameof(IsRowColumn));
        Regenerate();
    }

    private bool _msbFirst;
    public bool IsForward
    {
        get => _msbFirst;
        set { if (value && !_msbFirst) { _msbFirst = true; OnPropertyChanged(nameof(IsReversed)); Regenerate(); } }
    }

    public bool IsReversed
    {
        get => !_msbFirst;
        set { if (value && _msbFirst) { _msbFirst = false; OnPropertyChanged(nameof(IsForward)); Regenerate(); } }
    }

    private bool _positiveCode = true;
    public bool IsNegativeCode
    {
        get => !_positiveCode;
        set { if (value && _positiveCode) { _positiveCode = false; OnPropertyChanged(nameof(IsPositiveCode)); Regenerate(); } }
    }

    public bool IsPositiveCode
    {
        get => _positiveCode;
        set { if (value && !_positiveCode) { _positiveCode = true; OnPropertyChanged(nameof(IsNegativeCode)); Regenerate(); } }
    }

    private bool _decimal;
    public bool IsHex
    {
        get => !_decimal;
        set { if (value && _decimal) { _decimal = false; OnPropertyChanged(nameof(IsDecimal)); Regenerate(); } }
    }

    public bool IsDecimal
    {
        get => _decimal;
        set { if (value && !_decimal) { _decimal = true; OnPropertyChanged(nameof(IsHex)); Regenerate(); } }
    }

    private int _formatIndex;
    public int FormatIndex
    {
        get => _formatIndex;
        set
        {
            if (!SetProperty(ref _formatIndex, value)) return;
            // C51 / A51 切换时载入对应默认模板；自定义保留用户当前编辑内容
            if (value is 0 or 1) TemplateText = CodeTemplate.GetDefault(value);
            Regenerate();
        }
    }

    private int _bytesPerLine = 16;
    public int BytesPerLine
    {
        get => _bytesPerLine;
        set { if (SetProperty(ref _bytesPerLine, Math.Clamp(value, 1, 80))) Regenerate(); }
    }

    private bool _addCharComment;
    public bool AddCharComment
    {
        get => _addCharComment;
        set { if (SetProperty(ref _addCharComment, value)) Regenerate(); }
    }

    private string _inputText = "中";
    public string InputText
    {
        get => _inputText;
        set { if (SetProperty(ref _inputText, value ?? string.Empty)) Regenerate(); }
    }

    #endregion

    #region 输出

    private ImageSource? _previewImage;
    public ImageSource? PreviewImage
    {
        get => _previewImage;
        private set => SetProperty(ref _previewImage, value);
    }

    private ImageSource? _lcdImage;
    public ImageSource? LcdImage
    {
        get => _lcdImage;
        private set => SetProperty(ref _lcdImage, value);
    }

    private string _outputCode = string.Empty;
    public string OutputCode
    {
        get => _outputCode;
        private set => SetProperty(ref _outputCode, value);
    }

    private string _statusText = "就绪";
    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string CellSummary => $"{CellWidth} × {CellHeight}";

    #endregion

    private IFontRasterizer ActiveRasterizer => RasterizerIndex == 1 ? _skia : _gdi;

    private CodeFormatOptions BuildFormatOptions()
    {
        var options = FormatIndex switch
        {
            1 => CodeFormatOptions.CreateA51(),
            _ => CodeFormatOptions.CreateC51()
        };

        if (IsDecimal)
        {
            options.Decimal = true;
            options.BytePrefix = string.Empty;
            options.ByteSuffix = ",";
        }

        options.BytesPerLine = BytesPerLine;
        options.UpperCase = true;
        return options;
    }

    private FontSpec CreateFontSpec(string glyph)
    {
        // PCtoLCD2002 对半角字符使用半宽单元格，并以显式字宽压缩字形；
        // 实测 16×16 / 8×24 时与原软件逐字节一致。
        var isAscii = glyph.Length > 0 && glyph[0] < 0x80;
        var width = isAscii ? Math.Max(1, CellWidth / 2) : CellWidth;
        return new FontSpec
        {
            Family = SelectedFont,
            Width = width,
            Height = CellHeight,
            Bold = Bold,
            Italic = Italic,
            Underline = Underline,
            Charset = CharsetIndex == 0 ? 134 : 0,
            FixedWidth = FixedWidth,
            ExplicitWidth = isAscii ? width : 0,
            OffsetX = OffsetX,
            OffsetY = OffsetY
        };
    }

    private void Regenerate()
    {
        try
        {
            var chars = CharsetUtil.SplitChars(string.IsNullOrEmpty(InputText) ? " " : InputText);
            if (chars.Count > 512) chars = chars.Take(512).ToList();

            var scan = new ScanOptions
            {
                ScanMode = _scanMode,
                BitOrder = _msbFirst ? BitOrder.MsbFirst : BitOrder.LsbFirst,
                Invert = _positiveCode
            };

            var rasterizer = ActiveRasterizer;
            var glyphs = new List<MonoBitmap>(chars.Count);
            _glyphBytes.Clear();
            _glyphChars.Clear();

            foreach (var ch in chars)
            {
                var bitmap = rasterizer.Rasterize(ch, CreateFontSpec(ch));
                glyphs.Add(bitmap);
                _glyphBytes.Add(BitmapPacker.Pack(bitmap, scan));
                _glyphChars.Add(ch);
            }

            var formatOptions = BuildFormatOptions();
            var blocks = new List<string>(_glyphBytes.Count);
            for (var i = 0; i < _glyphBytes.Count; i++)
            {
                var trailing = FormatIndex == 1
                    ? $"{formatOptions.CommentPrefix}{_glyphChars[i]}{formatOptions.CommentSuffix}{i}"
                    : null;
                var block = CodeTemplate.Apply(
                    TemplateText, _glyphBytes[i], formatOptions,
                    glyphs[i].Width, glyphs[i].Height, _glyphChars[i], i, trailing);
                if (AddCharComment)
                    block = $"/* '{_glyphChars[i]}' */{Environment.NewLine}{block}";
                blocks.Add(block);
            }

            OutputCode = string.Join(Environment.NewLine, blocks);

            DemoBitmap = glyphs.FirstOrDefault();
            DemoBytes = _glyphBytes.FirstOrDefault() ?? Array.Empty<byte>();
            OnPropertyChanged(nameof(DemoBitmap));
            OnPropertyChanged(nameof(DemoBytes));

            var scale = 8;
            if (glyphs.Count * glyphs[0].Width > 0)
                scale = Math.Clamp(640 / Math.Max(1, glyphs.Count * glyphs[0].Width), 3, 16);

            PreviewImage = BitmapFactory.FromMonoStrip(
                glyphs, scale,
                Color.FromArgb(255, 0xE8, 0xEE, 0xF7),
                Color.FromArgb(255, 0x1B, 0x1E, 0x24),
                true,
                Color.FromArgb(255, 0x2B, 0x31, 0x3B));

            LcdImage = BitmapFactory.FromMonoStrip(
                glyphs, Math.Clamp(scale * 2, 6, 24),
                Color.FromArgb(255, 0x62, 0xE8, 0xB5),
                Color.FromArgb(255, 0x0A, 0x10, 0x0C),
                false,
                Color.FromArgb(255, 0x1B, 0x2A, 0x22));

            var totalBytes = _glyphBytes.Sum(b => b.Length);
            StatusText = $"{_glyphBytes.Count} 个字符，每字 {_glyphBytes.FirstOrDefault()?.Length ?? 0} 字节，共 {totalBytes} 字节";
        }
        catch (Exception ex)
        {
            StatusText = $"生成失败：{ex.Message}";
        }
    }

    private void CopyCode()
    {
        if (string.IsNullOrEmpty(OutputCode)) return;
        var package = new DataPackage();
        package.SetText(OutputCode);
        Clipboard.SetContent(package);
        Clipboard.Flush();
        StatusText = "已复制到剪贴板";
    }

    private async Task RunAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            StatusText = $"操作失败：{ex.Message}";
        }
    }

    private async Task SaveTextAsync()
    {
        var picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = "font_data"
        };
        PickerHelper.Initialize(picker);
        picker.FileTypeChoices.Add("C 头文件", new List<string> { ".h" });
        picker.FileTypeChoices.Add("文本文件", new List<string> { ".txt" });

        var file = await picker.PickSaveFileAsync();
        if (file is null) return;

        var ext = Path.GetExtension(file.Name);
        var content = ext.Equals(".txt", StringComparison.OrdinalIgnoreCase)
            ? OutputCode
            : $"#ifndef MODULUS_FONT_DATA_H{Environment.NewLine}#define MODULUS_FONT_DATA_H{Environment.NewLine}{Environment.NewLine}{OutputCode}{Environment.NewLine}{Environment.NewLine}#endif{Environment.NewLine}";

        File.WriteAllText(file.Path, content, new UTF8Encoding(false));
        StatusText = $"已保存 {file.Name}";
    }

    private async Task SaveBinaryAsync()
    {
        if (_glyphBytes.Count == 0) return;

        var picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = "font"
        };
        PickerHelper.Initialize(picker);
        picker.FileTypeChoices.Add("二进制字库", new List<string> { ".fon", ".bin" });

        var file = await picker.PickSaveFileAsync();
        if (file is null) return;

        using (var stream = File.Create(file.Path))
        {
            foreach (var bytes in _glyphBytes) stream.Write(bytes);
        }
        StatusText = $"已导出字库，共 {_glyphBytes.Sum(b => b.Length)} 字节";

        var indexPicker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = "font_index"
        };
        PickerHelper.Initialize(indexPicker);
        indexPicker.FileTypeChoices.Add("索引文件", new List<string> { ".txt" });

        var indexFile = await indexPicker.PickSaveFileAsync();
        if (indexFile is null) return;

        var sb = new StringBuilder();
        var record = 0;
        for (var i = 0; i < _glyphChars.Count; i++)
        {
            sb.AppendLine($"{_glyphChars[i]}({record})");
            record += _glyphBytes[i].Length;
        }
        File.WriteAllText(indexFile.Path, sb.ToString(), new UTF8Encoding(false));
    }

    private async Task SaveLibraryAsync()
    {
        var picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = "gb2312"
        };
        PickerHelper.Initialize(picker);
        picker.FileTypeChoices.Add("二进制字库", new List<string> { ".fon" });

        var file = await picker.PickSaveFileAsync();
        if (file is null) return;

        StatusText = "正在生成 GB2312 字库…";

        var chars = CharsetUtil.GetGb2312Hanzi();
        var spec = new FontSpec
        {
            Family = SelectedFont,
            Width = CellWidth,
            Height = CellHeight,
            Bold = Bold,
            Italic = Italic,
            Charset = 134,
            FixedWidth = true,
            OffsetX = OffsetX,
            OffsetY = OffsetY
        };
        var scan = new ScanOptions
        {
            ScanMode = _scanMode,
            BitOrder = _msbFirst ? BitOrder.MsbFirst : BitOrder.LsbFirst,
            Invert = _positiveCode
        };
        var rasterizer = ActiveRasterizer;

        using var stream = File.Create(file.Path);
        var index = new StringBuilder();
        var record = 0;
        foreach (var ch in chars)
        {
            var bytes = BitmapPacker.Pack(rasterizer.Rasterize(ch, spec), scan);
            stream.Write(bytes);
            index.AppendLine($"{ch}({record})");
            record += bytes.Length;
        }

        File.WriteAllText(Path.ChangeExtension(file.Path, ".txt"), index.ToString(), new UTF8Encoding(false));
        StatusText = $"已生成 GB2312 字库（{chars.Count} 字，{record} 字节）";
    }

    public void Dispose()
    {
        _gdi.Dispose();
        _skia.Dispose();
    }
}
